namespace IPMS.Models.DTOs.Reports
{
    public sealed record YoYAllocationRowDto(
    int Year,
    string InvestmentType,
    decimal TotalValue,
    decimal AllocationPercent
);

    public sealed record YoYAllocationReportDto(
    IReadOnlyList<YoYAllocationRowDto> Rows
);
}
