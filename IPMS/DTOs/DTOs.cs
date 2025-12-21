using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.DTOs
{
    public record RegisterUserDto(string Email, string FirstName, string? LastName, string Password);
    public record LoginUserDto(string Email, string Password);
    public record UserDto(Guid UserId, string Email, string FirstName, string? LastName, bool IsActive);
    public record LoginRequestDto(string Email, string Password);
    public record TokenResponseDto(string AccessToken, string RefreshToken);

}
