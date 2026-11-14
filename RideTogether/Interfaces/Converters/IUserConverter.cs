using RideTogether.Entities;
using RideTogether.Entities.DTO;

namespace RideTogether.Interfaces.Converters;

public interface IUserConverter
{
    UserDto ConvertFromModelToDto(User model); 
}