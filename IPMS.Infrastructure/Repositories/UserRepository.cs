using IPMS.Core.Entities;
using IPMS.Core.Entities.IPMS.Core.Entities;
using IPMS.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IPMS.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
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
            return User.Rehydrate(
                userId: reader.GetGuid(reader.GetOrdinal("UserId")),
                email: reader.GetString(reader.GetOrdinal("Email")),
                firstName: reader.GetString(reader.GetOrdinal("FirstName")),
                lastName: reader["LastName"] as string,
                passwordHash: (byte[])reader["PasswordHash"],
                passwordSalt: (byte[])reader["PasswordSalt"],
                isActive: reader.GetBoolean(reader.GetOrdinal("IsActive")),
                isDeleted: reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                updatedAt: reader["UpdatedAt"] as DateTime?,
                emailConfirmed: reader.GetBoolean(reader.GetOrdinal("EmailConfirmed"))
            );
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

        public async Task AddUserRoleAsync(Guid userId, string roleName)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // 1. Get RoleId via helper method
            var roleId = await GetRoleIdByNameAsync(conn, roleName);
            if (roleId == null)
            {
                throw new InvalidOperationException($"Role '{roleName}' does not exist.");
            }

            // 2. Check if user already has this role via helper
            var alreadyAssigned = await UserHasRoleAsync(conn, userId, roleId.Value);
            if (alreadyAssigned)
            {
                return;
            }

            // 3. Insert new UserRole
            var insertCmd = new SqlCommand(
                "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)", conn);
            insertCmd.Parameters.AddWithValue("@UserId", userId);
            insertCmd.Parameters.AddWithValue("@RoleId", roleId);

            await insertCmd.ExecuteNonQueryAsync();
        }

        public async Task RemoveUserRoleAsync(Guid userId, string roleName)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // 1. Get RoleId via helper method
            var roleId = await GetRoleIdByNameAsync(conn, roleName);
            if (roleId == null)
            {
                throw new InvalidOperationException($"Role '{roleName}' does not exist.");
            }

            // 2. Check if user actually has this role
            var hasRole = await UserHasRoleAsync(conn, userId, (int)roleId);
            if (!hasRole)
            {
                return;
            }

            // 3. Delete from UserRoles
            var deleteCmd = new SqlCommand(
                "DELETE FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId", conn);
            deleteCmd.Parameters.AddWithValue("@UserId", userId);
            deleteCmd.Parameters.AddWithValue("@RoleId", roleId);

            await deleteCmd.ExecuteNonQueryAsync();
        }

        public async Task RemoveRefreshTokenAsync(Guid userId, string refreshToken)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(
                "DELETE FROM RefreshTokens WHERE UserId = @UserId AND Token = @Token", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Token", refreshToken);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExistsAsync(Guid userId)
        {
            const string sql = @"
            SELECT COUNT(1)
            FROM Users
            WHERE UserId = @UserId
              AND IsDeleted = 0;";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            await conn.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync()! > 0;
        }

        public async Task ToggleActiveAsync(Guid userId)
        {
            const string sql = @"
            UPDATE Users
            SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserId = @UserId
              AND IsDeleted = 0;";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Helper method to get RoleId by role name.
        /// </summary>
        private async Task<int?> GetRoleIdByNameAsync(SqlConnection conn, string roleName)
        {
            var cmd = new SqlCommand("SELECT RoleId FROM Roles WHERE Name = @RoleName", conn);
            cmd.Parameters.AddWithValue("@RoleName", roleName);

            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            // Adjust cast depending on schema: Guid or int
            return (int)result;
        }

        /// <summary>
        /// Helper method to check if user already has a role.
        /// </summary>
        private async Task<bool> UserHasRoleAsync(SqlConnection conn, Guid userId, int roleId)
        {
            var cmd = new SqlCommand(
                "SELECT COUNT(1) FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@RoleId", roleId);

            var exists = (int)await cmd.ExecuteScalarAsync();
            return exists > 0;
        }

    }
}

