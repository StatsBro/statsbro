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
using System;

namespace StatsBro.Domain.Models.DTO
{
    public class MagicLinkDTO
    {
        public Guid Id { get; set; }

        public DateTime ValidTo { get; set; }

        public Guid UserId { get; set; }

        public MagicLinkOrigin Origin { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public enum MagicLinkOrigin
    {
        Login = 0,
        Email = 1,
    }
}
