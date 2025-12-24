using IPMS.Core.Entities;
using IPMS.Core.Entities.IPMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(Guid userId);
        Task RegisterAsync(User user);
        Task<IEnumerable<User>> GetAllAsync();
        Task UpdateAsync(User user);
        Task SoftDeleteAsync(Guid userId);
        Task<IEnumerable<string>> GetUserRolesAsync(Guid userId);
        //TODO: For below search and decide if we have to separate repository for the Refresh token methods 
        Task StoreRefreshTokenAsync(Guid userId, string token, DateTime expiresAt);
        Task<User?> GetUserByRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token);
        Task StoreOtpAsync(Guid userId, string otpType, string otpCode, DateTime expiry);
        Task<UserOtp?> GetValidOtpAsync(Guid userId, string otpCode, string otpType);
        Task MarkOtpUsedAsync(int otpId);
        Task ConfirmEmailAsync(Guid userId);
        Task AddUserRoleAsync(Guid userId, string roleName);
        Task RemoveUserRoleAsync(Guid userId, string roleName);
        Task RemoveRefreshTokenAsync(Guid userId, string refreshToken);
    }
}
