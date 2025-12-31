namespace IPMS.Models.DTOs.Reports
{
    public sealed record FileExport(
    byte[] Content,
    string ContentType,
    string FileName
);
}
