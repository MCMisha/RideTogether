using System.Data;
using Microsoft.EntityFrameworkCore;
using RideTogether.Context;
using RideTogether.Entities.Enums;
using RideTogether.Interfaces.Services;
using RideTogether.Models;

namespace RideTogether.Services;

public sealed class RideBookingService(RideTogetherDbContext context)
    : IRideBookingService
{
    public async Task<RideBookingResult> BookRideAsync(
        int rideId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var ride = await context.Rides
            .AsNoTracking()
            .SingleOrDefaultAsync(ride => ride.Id == rideId, cancellationToken);

        if (ride is null)
            return RideBookingResult.NotFound;

        if (ride.Info.UserId == userId)
            return RideBookingResult.Forbidden;

        var bookings = context.RideBookings
            .Where(booking => booking.RideId == rideId);

        if (await bookings.AnyAsync(
                booking => booking.UserId == userId, cancellationToken))
        {
            return RideBookingResult.AlreadyBooked;
        }

        var bookedSeats = await bookings.CountAsync(cancellationToken);
        if (bookedSeats >= ride.Info.Seats)
            return RideBookingResult.Full;

        context.RideBookings.Add(new RideBooking
        {
            RideId = rideId,
            UserId = userId
        });

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RideBookingResult.Success;
    }
}