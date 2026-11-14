using RideTogether.Entities;
using RideTogether.Entities.Enums;
using RideTogether.Models.DTO;
using RideTogether.Patterns.Maybe;

namespace RideTogether.Interfaces;

public interface IRideService
{
    Task<IEnumerable<RideResponseDto>> GetRides();
    Task<Maybe<Ride>> GetRideById(int id);
    Task<RideManageResultDto> CreateRide(RideDto rideDto, int userId);
    Task<RideManageResultDto> UpdateRide(RideDto rideDto, int userId);
    Task<RideManageResultDto> DeleteRideById(int id, int userId);
}