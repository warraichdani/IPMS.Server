using IPMS.Server.Extensions;
using System.Reflection;
using IPMS.Server.Models;

namespace IPMS.Server.Bindings
{
    public static class CurrentUserBinding
    {
        public static ValueTask<CurrentUser?> BindAsync(
            HttpContext httpContext,
            ParameterInfo parameter)
        {
            var userId = httpContext.GetUserId();

            if (userId is null)
                return ValueTask.FromResult<CurrentUser?>(null);

            return ValueTask.FromResult<CurrentUser?>(
                new CurrentUser(userId.Value)
            );
        }
    }
}
