using System.Data;
using Microsoft.Data.SqlClient;
using IPMS.Core.Entities;
using IPMS.Core.Interfaces;
using IPMS.Core.Entities.IPMS.Core.Entities;

namespace IPMS.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("SELECT * FROM Users WHERE Email = @Email AND IsDeleted = 0", conn);
            cmd.Parameters.AddWithValue("@Email", email);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }
            return null;
        }

        public async Task RegisterAsync(User user)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                INSERT INTO Users (UserId, Email, FirstName, LastName, PasswordHash, PasswordSalt, IsActive, IsDeleted, CreatedAt)
                VALUES (@UserId, @Email, @FirstName, @LastName, @PasswordHash, @PasswordSalt, @IsActive, @IsDeleted, @CreatedAt)", conn);

            cmd.Parameters.AddWithValue("@UserId", user.UserId);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
            cmd.Parameters.AddWithValue("@LastName", (object?)user.LastName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@PasswordSalt", user.PasswordSalt);
            cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
            cmd.Parameters.AddWithValue("@IsDeleted", user.IsDeleted);
            cmd.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            var users = new List<User>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("SELECT * FROM Users WHERE IsDeleted = 0", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }
            return users;
        }

        public async Task SoftDeleteAsync(Guid userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("UPDATE Users SET IsDeleted = 1 WHERE UserId = @UserId", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        private User MapUser(SqlDataReader reader)
        {
            return new User
            {
                UserId = reader.GetGuid(reader.GetOrdinal("UserId")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                LastName = reader["LastName"] as string,
                PasswordHash = (byte[])reader["PasswordHash"],
                PasswordSalt = (byte[])reader["PasswordSalt"],
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader["UpdatedAt"] as DateTime?
            };
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("SELECT * FROM Users WHERE UserId = @UserId AND IsDeleted = 0", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }
            return null;
        }

        public async Task UpdateAsync(User user)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            UPDATE Users
            SET FirstName = @FirstName,
                LastName = @LastName,
                Email = @Email,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE UserId = @UserId AND IsDeleted = 0", conn);

            cmd.Parameters.AddWithValue("@UserId", user.UserId);
            cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
            cmd.Parameters.AddWithValue("@LastName", (object?)user.LastName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(Guid userId)
        {
            var roles = new List<string>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            SELECT R.Name 
            FROM UserRoles UR
            INNER JOIN Roles R ON UR.RoleId = R.RoleId
            WHERE UR.UserId = @UserId", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                roles.Add(reader.GetString(0));
            }
            return roles;
        }

        public async Task StoreRefreshTokenAsync(Guid userId, string token, DateTime expiresAt)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            INSERT INTO RefreshTokens (UserId, Token, ExpiresAt, Revoked)
            VALUES (@UserId, @Token, @ExpiresAt, 0)", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Token", token);
            cmd.Parameters.AddWithValue("@ExpiresAt", expiresAt);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<User?> GetUserByRefreshTokenAsync(string token)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            SELECT U.* 
            FROM RefreshTokens RT
            INNER JOIN Users U ON RT.UserId = U.UserId
            WHERE RT.Token = @Token AND RT.Revoked = 0 AND RT.ExpiresAt > SYSUTCDATETIME()", conn);

            cmd.Parameters.AddWithValue("@Token", token);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }
            return null;
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("UPDATE RefreshTokens SET Revoked = 1 WHERE Token = @Token", conn);
            cmd.Parameters.AddWithValue("@Token", token);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task StoreOtpAsync(Guid userId, string otpType, string otpCode, DateTime expiry)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            INSERT INTO UserOtps (UserId, OtpType, OtpCode, ExpiryDateTime, IsUsed, CreatedAt)
            VALUES (@UserId, @OtpType, @OtpCode, @ExpiryDateTime, 0, @CreatedAt)", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@OtpType", otpType);
            cmd.Parameters.AddWithValue("@OtpCode", otpCode);
            cmd.Parameters.AddWithValue("@ExpiryDateTime", expiry);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<UserOtp?> GetValidOtpAsync(Guid userId, string otpCode, string otpType)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
            SELECT TOP 1 * 
            FROM UserOtps 
            WHERE UserId = @UserId 
              AND OtpCode = @OtpCode 
              AND OtpType = @OtpType 
              AND IsUsed = 0 
              AND ExpiryDateTime > @UTCNow", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@OtpCode", otpCode);
            cmd.Parameters.AddWithValue("@OtpType", otpType);
            cmd.Parameters.AddWithValue("@UTCNow", DateTime.UtcNow);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserOtp
                {
                    OtpId = reader.GetInt32(reader.GetOrdinal("OtpId")),
                    UserId = reader.GetGuid(reader.GetOrdinal("UserId")),
                    OtpType = reader.GetString(reader.GetOrdinal("OtpType")),
                    OtpCode = reader.GetString(reader.GetOrdinal("OtpCode")),
                    ExpiryDateTime = reader.GetDateTime(reader.GetOrdinal("ExpiryDateTime")),
                    IsUsed = reader.GetBoolean(reader.GetOrdinal("IsUsed")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                };
            }
            return null;
        }

        public async Task MarkOtpUsedAsync(int otpId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("UPDATE UserOtps SET IsUsed = 1 WHERE OtpId = @OtpId", conn);
            cmd.Parameters.AddWithValue("@OtpId", otpId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ConfirmEmailAsync(Guid userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("UPDATE Users SET EmailConfirmed = 1 WHERE UserId = @UserId", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

