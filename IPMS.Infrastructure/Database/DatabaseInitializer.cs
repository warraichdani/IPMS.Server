using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IPMS.Infrastructure.Database
{
    public sealed class DatabaseInitializer
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(
            IConfiguration config,
            ILogger<DatabaseInitializer> logger)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            var databaseName = builder.InitialCatalog;

            // Connect to master
            builder.InitialCatalog = "master";

            await using var masterConn = new SqlConnection(builder.ConnectionString);
            await masterConn.OpenAsync();

            // Check DB exists
            var existsCmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.databases WHERE name = @db",
                masterConn);

            existsCmd.Parameters.AddWithValue("@db", databaseName);

            var exists = (int)await existsCmd.ExecuteScalarAsync() > 0;

            if (!exists)
            {
                _logger.LogInformation("Database {Db} not found. Creating…", databaseName);

                var createCmd = new SqlCommand(
                    $"CREATE DATABASE [{databaseName}];",
                    masterConn);

                await createCmd.ExecuteNonQueryAsync();
                // Now run scripts
                await RunScriptsAsync();
            }
        }

        private async Task RunScriptsAsync()
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var script in SqlScriptRunner.LoadScripts())
            {
                _logger.LogInformation("Executing script: {Script}", script.Name);
                await SqlScriptRunner.ExecuteAsync(conn, script.Sql);
            }
        }
    }

}
