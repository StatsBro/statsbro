using System;

namespace StatsBro.Domain.Models.DTO
{
    public class SiteDTO
    {
        public string Id { get; set; } = null!;

        public string Domain { get; set; } = null!;

        public string? PersistQueryParams { get; set; }

        public string? IgnoreIPs { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
