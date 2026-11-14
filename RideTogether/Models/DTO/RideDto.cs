namespace RideTogether.Models.DTO;

public record RideDto(
    int Id,
    string Departure,
    string Destination,
    DateTime StartAt,
    int RideTime,
    string MeetPlace,
    int Seats);