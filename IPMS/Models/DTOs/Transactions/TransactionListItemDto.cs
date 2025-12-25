using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Models.DTOs.Transactions
{
    public sealed record TransactionListItemDto(
    Guid TransactionId,
    string TransactionType,
    decimal Units,
    decimal UnitPrice,
    decimal Amount,
    DateOnly TransactionDate,
    string? Notes
);
}
