namespace IPMS.Core.Application.Activity
{
    public sealed record ActivityEntry(
    Guid? ActorUserId,
    string Action,
    string? EntityType,
    string? EntityId,
    string? Summary,
    object? Details,
    string? IPAddress,
    DateTime OccurredAt
);
}
