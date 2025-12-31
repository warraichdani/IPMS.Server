namespace IPMS.Models.DTOs.Reports
{

    public sealed record ReportFiltersRequest(
    string From,
    string To,
    IReadOnlyList<string>? InvestmentTypes,
    int Page = 1,
    int PageSize = 30,
    bool ExportAll = false
)
    {
        public DateOnly FromDate => DateOnly.Parse(From);
        public DateOnly ToDate => DateOnly.Parse(To);
    }
}
