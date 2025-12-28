using IPMS.Core.Domain.Users;

namespace IPMS.Core.Entities
{
    public class User
    {
        private User() { }
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool EmailConfirmed { get; set; }


        public static User Create(
        string email,
        string firstName,
        string? lastName,
        string plainPassword,
        Func<string, (byte[] hash, byte[] salt)> passwordHasher)
        {
            // 🔒 Domain rule enforced here
            PasswordPolicy.Validate(plainPassword);

            var (hash, salt) = passwordHasher(plainPassword);

            return new User
            {
                UserId = Guid.NewGuid(),
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false,
                EmailConfirmed = false
            };
        }

        public static User Rehydrate(
        Guid userId,
        string email,
        string firstName,
        string? lastName,
        byte[] passwordHash,
        byte[] passwordSalt,
        bool isActive,
        bool isDeleted,
        DateTime createdAt,
        DateTime? updatedAt,
        bool emailConfirmed)
        {
            return new User
            {
                UserId = userId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                IsActive = isActive,
                IsDeleted = isDeleted,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                EmailConfirmed = emailConfirmed
            };
        }
    }
}
