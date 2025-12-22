using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Core.Entities
{
    namespace IPMS.Core.Entities
    {
        public class UserOtp
        {
            public int OtpId { get; set; }                // Primary key (INT IDENTITY)
            public Guid UserId { get; set; }              // FK to Users table
            public string OtpType { get; set; } = string.Empty; // 'EmailConfirmation', 'PhoneConfirmation'
            public string OtpCode { get; set; } = string.Empty; // e.g. "5332"
            public DateTime ExpiryDateTime { get; set; }  // Expiry timestamp
            public bool IsUsed { get; set; }              // Flag for usage
            public DateTime CreatedAt { get; set; }       // Audit timestamp

            // Domain behavior
            public bool IsValid(string code, string type)
            {
                return !IsUsed &&
                       OtpCode == code &&
                       OtpType == type &&
                       ExpiryDateTime > DateTime.UtcNow;
            }

            public void MarkUsed()
            {
                IsUsed = true;
            }
        }
    }
}
