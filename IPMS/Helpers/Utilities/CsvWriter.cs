using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Helpers.Utilities
{
    public static class CsvWriter
    {
        public static byte[] Write<T>(
            IEnumerable<T> rows,
            IReadOnlyList<string> headers,
            Func<T, IEnumerable<string>> map)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers));

            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",", map(row)));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
