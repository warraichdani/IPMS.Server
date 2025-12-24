using IPMS.Core.Entities;
using IPMS.Core.Interfaces;
using IPMS.DTOs;
using IPMS.Services.IPMS.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IPMS.Services
{
    public interface IAuthService
    {
        Task<TokenResponseDto?> LoginAsync(LoginRequestDto dto);
        Task<TokenResponseDto?> RefreshAsync(string refreshToken);
        Task LogoutAsync(Guid userId, string refreshToken);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _config;
        private readonly IEventLogger<AuthService> _logger;

        public AuthService(IUserRepository userRepo, IConfiguration config, IEventLogger<AuthService> logger)
        {
            _userRepo = userRepo;
            _config = config;
            _logger = logger;
        }

        public async Task<TokenResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userRepo.GetByEmailAsync(dto.Email);
            if (user == null || user.IsDeleted)
            {
                _logger.LogWarning($"Login failed: user with email {dto.Email} not found.");
                return null;
            }

            if (!user.EmailConfirmed)
            {
                _logger.LogWarning($"Login failed: user with email {dto.Email} email is not confirmed yet.");
                return null;
            }

            // Validate password
            using var hmac = new System.Security.Cryptography.HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));
            if (!computedHash.SequenceEqual(user.PasswordHash))
            {
                _logger.LogWarning($"Login failed: invalid password for user {dto.Email}.");
                return null;
            }
            // Fetch roles from DB
            var roles = await _userRepo.GetUserRolesAsync(user.UserId);
            if (!roles.Any()) roles = new[] { "User" }; // fallback

            _logger.LogInfo($"User {dto.Email} logged in successfully at {DateTime.UtcNow}.");

            // Generate Access Token
            var accessToken = GenerateJwtToken(user, roles);

            // Generate Refresh Token
            var refreshToken = Guid.NewGuid().ToString("N"); // simple string token
            await _userRepo.StoreRefreshTokenAsync(user.UserId, refreshToken, DateTime.UtcNow.AddDays(7));

            return new TokenResponseDto(accessToken, refreshToken);
        }

        private string GenerateJwtToken(User user, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<TokenResponseDto?> RefreshAsync(string refreshToken)
        {
            var user = await _userRepo.GetUserByRefreshTokenAsync(refreshToken);
            if (user == null) return null;

            // revoke old refresh token
            await _userRepo.RevokeRefreshTokenAsync(refreshToken);

            // fetch roles
            var roles = await _userRepo.GetUserRolesAsync(user.UserId);

            // generate new tokens
            var accessToken = GenerateJwtToken(user, roles);
            var newRefreshToken = Guid.NewGuid().ToString("N");
            await _userRepo.StoreRefreshTokenAsync(user.UserId, newRefreshToken, DateTime.UtcNow.AddDays(7));

            return new TokenResponseDto(accessToken, newRefreshToken);
        }

        public async Task LogoutAsync(Guid userId, string refreshToken)
        {
            await _userRepo.RemoveRefreshTokenAsync(userId, refreshToken);
            _logger.LogInfo($"User {userId} logged out. Refresh token removed at {DateTime.UtcNow}.");
        }
    }
}