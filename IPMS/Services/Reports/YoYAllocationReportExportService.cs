
using IPMS.Helpers.Utilities;
using IPMS.Models.DTOs.Reports;
using System.Text;
using System.Text.Json;

namespace IPMS.Services.Reports
{
    public sealed class YoYAllocationReportExportService
    : IYoYAllocationReportExportService
    {
        public FileExport ExportCsv(IEnumerable<YoYAllocationRowDto> rows)
        {
            var csv = CsvWriter.Write(
                rows,
                new[]
                {
                "Year",
                "Investment Type",
                "Total Value",
                "Allocation %"
                },
                r => new[]
                {
                r.Year.ToString(),
                r.InvestmentType,
                r.TotalValue.ToString("0.00"),
                r.AllocationPercent.ToString("0.00")
                });

            return new FileExport(
                csv,
                "text/csv",
                $"yoy_allocation_{DateTime.UtcNow:yyyyMMdd}.csv"
            );
        }

        private static string Escape(string value)
            => value.Contains(',')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;

        public FileExport ExportJson(IEnumerable<YoYAllocationRowDto> rows)
        {
            var json = JsonSerializer.Serialize(
                rows,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            return new FileExport(
                Encoding.UTF8.GetBytes(json),
                "application/json",
                $"yoy_allocation_{DateTime.UtcNow:yyyyMMdd}.json"
            );
        }
        public FileExport ExportPdf(IEnumerable<YoYAllocationRowDto> rows)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Year-over-Year Allocation Report");
            sb.AppendLine("--------------------------------");
            sb.AppendLine();

            foreach (var group in rows.GroupBy(r => r.Year))
            {
                sb.AppendLine($"Year: {group.Key}");
                sb.AppendLine("Type\tValue\tAllocation %");

                foreach (var r in group)
                {
                    sb.AppendLine(
                        $"{r.InvestmentType}\t" +
                        $"{r.TotalValue:0.00}\t" +
                        $"{r.AllocationPercent:0.00}%"
                    );
                }

                sb.AppendLine();
            }

            return new FileExport(
                Encoding.UTF8.GetBytes(sb.ToString()),
                "application/pdf",
                $"yoy_allocation_{DateTime.UtcNow:yyyyMMdd}.pdf"
            );
        }
    }
}
