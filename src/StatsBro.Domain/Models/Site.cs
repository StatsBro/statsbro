using System;
using System.Collections.Generic;
using System.Net;

namespace StatsBro.Domain.Models
{
    public class Site
    {
        public Guid Id { get; set; }

        public string Domain { get; set; } = null!;

        public Guid UserId { get; set; }

        public bool IsScriptLive { get; set; }

        public List<string> PersistQueryParamsList { get; set; } = new List<string>();

        public List<IPAddress> IgnoreIPsList { get; set; } = new List<IPAddress>();

        public DateTime CreatedAt { get; set; }
    }
}
