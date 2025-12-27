using IPMS.Models.DTOs.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Dashboard
{
    public interface IUserDashboardQuery
    {
        DashboardSummaryDto Get(Guid userId);
    }
}
