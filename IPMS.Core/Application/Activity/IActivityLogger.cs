
namespace IPMS.Core.Application.Activity
{
    public interface IActivityLogger
    {
        Task LogAsync(ActivityEntry entry);

        Task LogAsync(List<ActivityEntry> list);
    }
}
