using System.Data;
using Microsoft.Data.SqlClient;
using IPMS.Core.Entities;
using IPMS.Core.Interfaces;

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
    }
}

