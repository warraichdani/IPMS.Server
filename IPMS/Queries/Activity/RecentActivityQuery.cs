using IPMS.Models.DTOs.Activity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Activity
{
    public sealed class RecentActivityQuery : IRecentActivityQuery
    {
        private readonly string _connectionString;

        public RecentActivityQuery(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public async Task<IReadOnlyList<RecentActivityDto>> GetRecent(int limit = 10)
        {
            const string sql = @"
            SELECT TOP (@Limit)
                ActorUserId,
                Action,
                Summary,
                OccurredAt
            FROM ActivityLog
            ORDER BY OccurredAt DESC;
            ";

            var result = new List<RecentActivityDto>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@Limit", limit);

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new RecentActivityDto(
                    reader.IsDBNull(0) ? null : reader.GetGuid(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetDateTime(3)
                ));
            }

            return result;
        }
    }
}
