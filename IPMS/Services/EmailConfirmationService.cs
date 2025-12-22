using IPMS.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Services
{
    public interface IEmailConfirmationService
    {
        Task<bool> ConfirmEmailAsync(string email, string otpCode);
    }

    public class EmailConfirmationService : IEmailConfirmationService
    {
        private readonly IUserRepository _userRepo;

        public EmailConfirmationService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<bool> ConfirmEmailAsync(string email, string otpCode)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null) return false;

            var otp = await _userRepo.GetValidOtpAsync(user.UserId, otpCode, "EmailConfirmation");
            if (otp == null) return false;

            await _userRepo.MarkOtpUsedAsync(otp.OtpId);
            await _userRepo.ConfirmEmailAsync(user.UserId);

            return true;
        }
    }
}
