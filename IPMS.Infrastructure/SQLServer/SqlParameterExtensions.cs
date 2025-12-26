using Microsoft.Data.SqlClient;
namespace IPMS.Infrastructure.SQLServer
{
    public static class SqlParameterExtensions
    {
        public static void AddNullable(
            this SqlParameterCollection parameters,
            string name,
            object? value)
        {
            parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }
}
