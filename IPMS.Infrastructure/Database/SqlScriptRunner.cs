using Microsoft.Data.SqlClient;

namespace IPMS.Infrastructure.Database
{
    public static class SqlScriptRunner
    {
        public static IEnumerable<(string Name, string Sql)> LoadScripts()
        {
            var basePath = Path.Combine(AppContext.BaseDirectory, "Scripts");

            foreach (var file in Directory.GetFiles(basePath, "*.sql"))
            {
                yield return (Path.GetFileName(file), File.ReadAllText(file));
            }
        }

        public static async Task ExecuteAsync(SqlConnection conn, string script)
        {
            var batches = script
                .Split(new[] { "\r\nGO\r\n", "\nGO\n" },
                       StringSplitOptions.RemoveEmptyEntries);

            foreach (var batch in batches)
            {
                using var cmd = new SqlCommand(batch, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

}
