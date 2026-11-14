namespace RideTogether.Models.DTO;

public record RideResponseDto(
    int Id,
    string Departure,
    string Destination,
    string StartAt,
    int RideTime,
    string MeetPlace,
    int Seats,
    int AvailableSeats,
    int UserId);