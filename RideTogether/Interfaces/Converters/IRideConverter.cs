using RideTogether.Entities;
using RideTogether.Models.DTO;

namespace RideTogether.Interfaces.Converters;

public interface IRideConverter
{
    Ride FromDtoToModel(RideDto dto);
    RideResponseDto FromModelToResponseDto(Ride model, int bookedSeats = 0);
}