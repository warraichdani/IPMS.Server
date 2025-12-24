using IPMS.DTOs;

namespace IPMS.Queries.Allocation;

public interface IAllocationQuery
{
    IReadOnlyList<AllocationDto> GetByUser(Guid userId);
}

