using RideTogether.Entities.Enums;

namespace RideTogether.Interfaces.Services;

public interface IRideBookingService
{
    Task<RideBookingResult> BookRideAsync(
        int rideId,
        int userId,
        CancellationToken cancellationToken = default);
}