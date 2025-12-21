using IPMS.Core.Entities;
using IPMS.Core.Interfaces;
using IPMS.DTOs;
using System.Security.Cryptography;
using System.Text;

namespace IPMS.Services
{
    public interface IUserService
    {
        Task<UserDto> RegisterAsync(RegisterUserDto dto);
        Task<UserDto?> LoginAsync(LoginUserDto dto);
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task SoftDeleteAsync(Guid userId);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
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

            return new UserDto(user.UserId, user.Email, user.FirstName, user.LastName, user.IsActive);
        }

        public async Task<UserDto?> LoginAsync(LoginUserDto dto)
        {
            var user = await _repo.GetByEmailAsync(dto.Email);
            if (user == null) return null;

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));

            if (!computedHash.SequenceEqual(user.PasswordHash)) return null;

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


