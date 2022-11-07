using System;

namespace StatsBro.Domain.Models
{
    public class User
    {
        // nie wiem czy chcesz guid czy cos innego zrobilem guid
        public Guid Id { get; set; }

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string PasswordSalt { get; set; } = null!;

        public DateTimeOffset RegisteredAt { get; set; }
    }
}
