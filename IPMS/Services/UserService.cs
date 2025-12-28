using IPMS.Core.Entities;
using IPMS.Core.Interfaces;
using IPMS.Models.DTOs;
using IPMS.Models.Filters;
using IPMS.Services.IPMS.Services;
using IPMS.Shared;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace IPMS.Services
{
    public interface IUserService
    {
        Task<UserDto> RegisterAsync(RegisterUserDto dto);
        Task<PagedResult<UserDto>> GetAllAsync(UserListFilter filter);
        Task SoftDeleteAsync(Guid userId);
        Task ToggleActiveAsync(Guid userId);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly IEventLogger<UserService> _logger;

        public UserService(IUserRepository repo, IEventLogger<UserService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<UserDto> RegisterAsync(RegisterUserDto dto)
        {
            using var hmac = new HMACSHA512();
            var user = User.Create(
                dto.Email,
                dto.FirstName,
                dto.LastName,
                dto.Password,
                password =>
                {
                    using var hmac = new HMACSHA512();
                    return (
                        hmac.ComputeHash(Encoding.UTF8.GetBytes(password)),
                        hmac.Key
                    );
                });

            await _repo.RegisterAsync(user);

            // Simulation: insert OTP with fixed value "5332"
            await _repo.StoreOtpAsync(user.UserId, "EmailConfirmation", "533222", DateTime.UtcNow.AddMinutes(10));

            _logger.LogInfo($"New user registered: {user.Email} at {DateTime.UtcNow}.");
            return new UserDto(user.UserId, user.Email, user.FirstName, user.LastName, user.IsActive);
        }

        public async Task<PagedResult<UserDto>> GetAllAsync(UserListFilter filter)
        {
            var users = await _repo.GetAllAsync();

            // 🔍 Search (Email, FirstName, LastName)
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();

                users = users.Where(u =>
                    u.Email.ToLower().Contains(search) ||
                    u.FirstName.ToLower().Contains(search) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(search))
                );
            }

            var totalCount = users.Count();

            var items = users
                .OrderBy(u => u.FirstName)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(u => new UserDto(
                    u.UserId,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.IsActive))
                .ToList();

            return new PagedResult<UserDto>(
                items,
                totalCount,
                filter.Page,
                filter.PageSize);
        }

        public async Task SoftDeleteAsync(Guid userId) => await _repo.SoftDeleteAsync(userId);

        public async Task ToggleActiveAsync(Guid userId)
        {
            var exists = await _repo.ExistsAsync(userId);
            if (!exists)
                throw new InvalidOperationException("User not found.");

            await _repo.ToggleActiveAsync(userId);
        }
    }
}


