namespace RideTogether.Models;

public sealed class RideBooking
{
    public int Id { get; set; }
    public int RideId { get; set; }
    public int UserId { get; set; }
}