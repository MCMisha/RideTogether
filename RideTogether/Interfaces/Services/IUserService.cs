using RideTogether.Entities;
using RideTogether.Entities.DTO;

namespace RideTogether.Interfaces;

public interface IUserService
{
    Task<UserDto?> Login(string login, string password);
    Task<UserDto?> Register(string username, string password);
}