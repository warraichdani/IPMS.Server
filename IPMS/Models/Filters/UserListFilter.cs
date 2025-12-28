using System;
namespace IPMS.Models.Filters
{
    public sealed class UserListFilter
    {
        public string? Search { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }
}
