using IPMS.Core.Entities;
using IPMS.Core.Interfaces;
using IPMS.DTOs;
using IPMS.Services.IPMS.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace IPMS.Services
{
    public interface IUserService
    {
        Task<UserDto> RegisterAsync(RegisterUserDto dto);
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task SoftDeleteAsync(Guid userId);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly IEventLogger _logger;

        public UserService(IUserRepository repo, IEventLogger logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<UserDto> RegisterAsync(RegisterUserDto dto)
        {
            using var hmac = new HMACSHA512();
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)),
                PasswordSalt = hmac.Key,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.RegisterAsync(user);

            // Simulation: insert OTP with fixed value "5332"
            await _repo.StoreOtpAsync(user.UserId, "EmailConfirmation", "5332", DateTime.UtcNow.AddMinutes(10));

            _logger.LogInfo($"New user registered: {user.Email} at {DateTime.UtcNow}.");
            return new UserDto(user.UserId, user.Email, user.FirstName, user.LastName, user.IsActive);
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _repo.GetAllAsync();
            return users.Select(u => new UserDto(u.UserId, u.Email, u.FirstName, u.LastName, u.IsActive));
        }

        public async Task SoftDeleteAsync(Guid userId) => await _repo.SoftDeleteAsync(userId);
    }
}


