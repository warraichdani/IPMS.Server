using IPMS.Core.Application.DTOs;
using IPMS.Core.Application.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IPMS.Infrastructure.Repositories
{
    public sealed class SystemStatisticsRepository : ISystemStatisticsRepository
    {
        private readonly string _connectionString;

        public SystemStatisticsRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public async Task<SystemStatisticsDto> GetAsync()
        {
            const string sql = @"
-- Total users (all roles)
SELECT COUNT(*) 
FROM Users 
WHERE IsDeleted = 0;

-- Total portfolios (users with role 'user')
SELECT COUNT(DISTINCT ur.UserId)
FROM UserRoles ur
JOIN Roles r ON r.RoleId = ur.RoleId
JOIN Users u ON u.UserId = ur.UserId
WHERE r.Name = 'user'
  AND u.IsDeleted = 0;

-- Total investments current value
SELECT ISNULL(SUM(i.TotalUnits * i.UnitPrice), 0)
FROM Investments i
WHERE i.IsDeleted = 0;

-- Today's transactions (CreatedAt)
SELECT COUNT(*)
FROM Transactions
WHERE CAST(CreatedAt AS date) = CAST(SYSUTCDATETIME() AS date);
";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            await reader.ReadAsync();
            var totalUsers = reader.GetInt32(0);

            await reader.NextResultAsync();
            await reader.ReadAsync();
            var totalPortfolios = reader.GetInt32(0);

            await reader.NextResultAsync();
            await reader.ReadAsync();
            var totalInvestmentValue = reader.GetDecimal(0);

            await reader.NextResultAsync();
            await reader.ReadAsync();
            var todaysTransactions = reader.GetInt32(0);

            return new SystemStatisticsDto(
                totalUsers,
                totalPortfolios,
                totalInvestmentValue,
                todaysTransactions
            );
        }
    }
}
