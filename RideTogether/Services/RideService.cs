using Microsoft.EntityFrameworkCore;
using RideTogether.Context;
using RideTogether.Entities;
using RideTogether.Interfaces;
using RideTogether.Interfaces.Converters;
using RideTogether.Models.DTO;
using RideTogether.Models.Enums;
using RideTogether.Patterns.Maybe;

namespace RideTogether.Services;

public class RideService : IRideService
{
    private readonly RideTogetherDbContext _context;
    private readonly IRideConverter _converter;

    public RideService(RideTogetherDbContext context, IRideConverter converter)
    {
        _context = context;
        _converter = converter;
    }

    public async Task<IEnumerable<RideResponseDto>> GetRides()
    {
        var ridesWithSeats = await _context.Rides
            .AsNoTracking()
            .Select(ride => new
            {
                Ride = ride,
                BookedSeats = _context.RideBookings.Count(booking => booking.RideId == ride.Id)
            })
            .ToListAsync();

        return ridesWithSeats.Select(ride =>
            _converter.FromModelToResponseDto(ride.Ride, ride.Ride.Info.Seats - ride.BookedSeats));
    }

    public async Task<Maybe<Ride>> GetRideById(int id)
    {
        return Maybe<Ride>.Some(await _context.Rides.FirstOrDefaultAsync(x => x.Id == id));
    }

    public async Task<RideManageResultDto> CreateRide(RideDto rideDto, int userId)
    {
        Ride ride = _converter.FromDtoToModel(rideDto);
        ride.Info.UserId = userId;
        _context.Rides.Add(ride);
        await _context.SaveChangesAsync();
        var responseDto = _converter.FromModelToResponseDto(ride);
        return new RideManageResultDto(RideManageResult.Created, responseDto);
    }

    public async Task<RideManageResultDto> UpdateRide(RideDto rideDto, int userId)
    {
        var rideMaybe = await GetRideById(rideDto.Id);
        if (!rideMaybe.HasValue)
            return new RideManageResultDto(RideManageResult.NotFound);

        var existingRide = rideMaybe.Value;
        if (existingRide.Info.UserId != userId)
            return new RideManageResultDto(RideManageResult.Forbidden);
        Ride ride = _converter.FromDtoToModel(rideDto);
        existingRide.Departure = ride.Departure;
        existingRide.Destination = ride.Destination;
        existingRide.Info.StartAt = ride.Info.StartAt;
        existingRide.Info.MeetPlace = ride.Info.MeetPlace;
        existingRide.Info.RideTime = ride.Info.RideTime;
        existingRide.Info.Seats = ride.Info.Seats;

        await _context.SaveChangesAsync();

        var rideResponse = _converter.FromModelToResponseDto(existingRide);
        return new RideManageResultDto(RideManageResult.Updated, rideResponse);
    }

    public async Task<RideManageResultDto> DeleteRideById(int id, int userId)
    {
        var rideMaybe = await GetRideById(id);
        if (!rideMaybe.HasValue)
            return new RideManageResultDto(RideManageResult.NotFound);

        var ride = rideMaybe.Value;

        if (ride.Info.UserId != userId)
            return new RideManageResultDto(RideManageResult.Forbidden);

        _context.Rides.Remove(ride);
        await _context.SaveChangesAsync();
        var responseDto = _converter.FromModelToResponseDto(ride);
        return new RideManageResultDto(RideManageResult.Deleted, responseDto);
    }
}