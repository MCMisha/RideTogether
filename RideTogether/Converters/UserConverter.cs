using RideTogether.Entities;
using RideTogether.Entities.DTO;
using RideTogether.Interfaces.Converters;

namespace RideTogether.Converters;

public class UserConverter : IUserConverter
{
    public UserDto ConvertFromModelToDto(User model)
    {
        return new UserDto(model.Id, model.FirstName, model.LastName, model.Login);
    }
}

