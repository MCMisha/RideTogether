namespace RideTogether.Models;

public sealed record RideSearchFilters(
    string? Departure,
    string? Destination,
    string? Date,
    string? After);
