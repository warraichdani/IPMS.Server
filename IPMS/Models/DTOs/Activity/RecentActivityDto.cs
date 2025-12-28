namespace IPMS.Models.DTOs.Activity
{
    public sealed record RecentActivityDto(
    Guid? UserId,
    string Action,
    string? Summary,
    DateTime OccurredAt
);
}
