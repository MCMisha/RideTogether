using RideTogether.Entities;
using RideTogether.Interfaces.Converters;
using RideTogether.Models;
using RideTogether.Models.DTO;

namespace RideTogether.Converters;

public class RideConverter : IRideConverter
{
    public Ride FromDtoToModel(RideDto dto)
    {
        return new()
        {
            Id = dto.Id,
            Departure = dto.Departure,
            Destination = dto.Destination,
            Info = new RideInfo
            {
                MeetPlace = dto.MeetPlace,
                StartAt = dto.StartAt.ToString(
                    "yyyy-MM-dd'T'HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture),
                RideTime = dto.RideTime,
                Seats = dto.Seats
            }
        };
    }

    public RideResponseDto FromModelToResponseDto(Ride model, int bookedSeats = 0)
    {
        return new RideResponseDto(model.Id,model.Departure, model.Destination,
            model.Info.StartAt, model.Info.RideTime, model.Info.MeetPlace, model.Info.Seats, bookedSeats, model.Info.UserId);
    }
}