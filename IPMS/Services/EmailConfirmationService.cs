using IPMS.Core.Configs;
using IPMS.Core.Interfaces;
using IPMS.Services.IPMS.Services;
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
        private readonly AppConfigs appConfigs;
        private readonly IEventLogger<EmailConfirmationService> _logger;

        public EmailConfirmationService(IUserRepository userRepo, IEventLogger<EmailConfirmationService> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<bool> ConfirmEmailAsync(string email, string otpCode)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning($"Email confirmation failed: user {email} not found.");
                return false;
            }

            var otp = await _userRepo.GetValidOtpAsync(user.UserId, otpCode, "EmailConfirmation");
            if (otp == null)
            {
                _logger.LogWarning($"Email confirmation failed: invalid OTP for user {email}.");
                return false;
            }

            await _userRepo.MarkOtpUsedAsync(otp.OtpId);
            await _userRepo.ConfirmEmailAsync(user.UserId);

            _logger.LogInfo($"Email confirmed for user {email} at {DateTime.UtcNow}.");

            return true;
        }
    }
}
