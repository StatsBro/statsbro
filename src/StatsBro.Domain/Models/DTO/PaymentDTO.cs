﻿/* Copyright StatsBro.io and/or licensed to StatsBro.io under one
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
    public class PaymentDTO
    {
        public string Id { get; set; } = null!;

        public string IdFromProvider { get; set; } = null!;

        public PaymentProvider Provider { get; set; }

        public string OrganizationId { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public PaymentStatus Status { get; set; }

        public PaymentSource Source { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
