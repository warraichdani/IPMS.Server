namespace IPMS.Server.Models
{
    public record ConfirmEmailRequest(string Email, string Otp);
    public record LogoutRequest(string RefreshToken);

}
