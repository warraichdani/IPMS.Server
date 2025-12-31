using IPMS.Models.DTOs.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Reports
{
    public interface IYearOverYearAllocationQuery
    {
        IReadOnlyList<YoYAllocationRowDto> GetByUser(Guid userId);
    }
}
