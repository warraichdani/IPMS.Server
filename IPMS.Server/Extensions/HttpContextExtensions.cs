using System.Security.Claims;

namespace IPMS.Server.Extensions
{
    public static class HttpContextExtensions
    {
        public static Guid? GetUserId(this HttpContext context)
        {
            var subClaim = context.User.FindFirst(ClaimTypes.NameIdentifier) ?? context.User.FindFirst("sub");
            if (subClaim == null) return null;

            return Guid.TryParse(subClaim.Value, out var userId) ? userId : null;
        }
    }
}
