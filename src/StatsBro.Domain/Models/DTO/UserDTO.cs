using System;

namespace StatsBro.Domain.Models.DTO
{
    public class UserDTO
    {
        public string Id { get; set; } = null!;

        public string Email{ get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string PasswordSalt { get; set; } = null!;

        public DateTime RegisteredAt{ get; set; }
    }
}
