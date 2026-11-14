using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RideTogether.Context;
using RideTogether.Entities;
using RideTogether.Entities.DTO;
using RideTogether.Interfaces;
using RideTogether.Interfaces.Converters;

namespace RideTogether.Services;

public class UserService : IUserService
{
    private readonly RideTogetherDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUserConverter _userConverter;
    
    public UserService(RideTogetherDbContext context, IPasswordHasher<User> passwordHasher, IUserConverter userConverter)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _userConverter = userConverter;
    }

    public async Task<UserDto?> Login(string login, string password)
    {
        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        User? user = await _context.Users.FirstOrDefaultAsync(user => user.Login.Equals(login));
        if (user == null)
        {
            return null;
        }
        PasswordVerificationResult res = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
        if (res.Equals(PasswordVerificationResult.Failed))
        {
            return null;
        }
        
        UserDto userDto = _userConverter.ConvertFromModelToDto(user);
        return userDto;
    }

    public Task<UserDto?> Register(string username, string password)
    {
        throw new NotImplementedException();
    }
}