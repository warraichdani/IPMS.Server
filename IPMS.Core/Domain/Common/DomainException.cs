
namespace IPMS.Core.Domain.Users;
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
