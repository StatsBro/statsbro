/* Copyright StatsBro.io and/or licensed to StatsBro.io under one
 * or more contributor license agreements.
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the Server Side Public License, version 1

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * Server Side Public License for more details.

 * You should have received a copy of the Server Side Public License
 * along with this program. If not, see
 * <https://github.com/StatsBro/statsbro/blob/main/LICENSE>.
 */
namespace StatsBro.Domain.Service.PayU
{
    public class PayuConfig
    {
        public string BaseUrl { get; set; } = null!;

        public string ClientId { get; set; } = null!;

        public string ClientSecret { get; set; } = null!;

        /// <summary>
        /// This is the palce when user lands back from PayU payment process
        /// </summary>
        public string ContinueUrl { get; set; } = null!;
        
        public string NotifyUrl { get; set; } = null!;

        public string? NotifyAllowedIPs { get; set; }
    }
}
