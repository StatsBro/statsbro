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
using System.Text.Json.Serialization;

namespace StatsBro.Domain.Service.PayU.Models
{
    public class AuthorizeResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]

        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]

        public long ExpiresIn { get; set; }

        [JsonPropertyName("grand_type")]

        public string? GerandType { get; set; }
    }
}
