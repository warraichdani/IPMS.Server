using System.Text.RegularExpressions;

namespace IPMS.Core.Domain.Users;

public static class PasswordPolicy
{
    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new DomainException("Password cannot be empty.");

        if (password.Length < 8)
            throw new DomainException("Password must be at least 8 characters long.");

        if (!Regex.IsMatch(password, "[A-Z]"))
            throw new DomainException("Password must contain at least one uppercase letter.");

        if (!Regex.IsMatch(password, "[a-z]"))
            throw new DomainException("Password must contain at least one lowercase letter.");

        if (!Regex.IsMatch(password, "[0-9]"))
            throw new DomainException("Password must contain at least one digit.");
    }
}