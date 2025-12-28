using IPMS.Core.Application.Activity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace IPMS.Infrastructure.Repositories.Application
{
    public sealed class SqlActivityLogger : IActivityLogger
    {
        private readonly string _connectionString;

        public SqlActivityLogger(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("Default")!;
        }

        public async Task LogAsync(ActivityEntry entry)
        {
            const string sql = @"
INSERT INTO ActivityLog
(ActorUserId, Action, EntityType, EntityId, Summary, DetailsJson, IPAddress)
VALUES
(@UserId, @Action, @EntityType, @EntityId, @Summary, @DetailsJson, @IP);";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@UserId", (object?)entry.ActorUserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Action", entry.Action);
            cmd.Parameters.AddWithValue("@EntityType", (object?)entry.EntityType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EntityId", (object?)entry.EntityId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Summary", (object?)entry.Summary ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DetailsJson",
                entry.Details is null
                    ? DBNull.Value
                    : JsonSerializer.Serialize(entry.Details));
            cmd.Parameters.AddWithValue("@IP", (object?)entry.IPAddress ?? DBNull.Value);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }

}
