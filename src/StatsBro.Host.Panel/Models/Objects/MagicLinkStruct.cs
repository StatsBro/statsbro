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
using StatsBro.Domain.Helpers;

namespace StatsBro.Host.Panel.Models.Objects;

public class MagicLinkStruct
{
    public const string Separator = ";";
    
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public long Timestamp { get; set; }

    public override string ToString() => string.Join(Separator, Id.ToString("N"), Email, Timestamp);

    public static MagicLinkStruct? FromString(string input)
    {
        if(string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var parts = input.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        var maybeEmail = parts[1][..Math.Min(parts[1].Length, Validators.EmailMaxLength)];

        if (
            parts.Length != 3
            || !Guid.TryParse(parts[0], out var id)
            || string.IsNullOrEmpty(maybeEmail)
            || !long.TryParse(parts[2], out var timestamp)
            )
        {
            return null;
        }

        return new MagicLinkStruct 
        {
            Id = id,
            Email = maybeEmail,
            Timestamp = timestamp,
        };        
    }
}
