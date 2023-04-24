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
    public class PaymentTransactionDTO
    {
        public string Id { get; set; } = null!;

        public string OrganizationId { get; set; } = null!;

        public string PaymentId { get; set; } = null!;

        public decimal AmountNet { get; set; }

        public decimal AmountGross { get; set; }

        public decimal VatValue { get; set; }

        public decimal VatAmount { get; set; }

        public string Currency { get; set; } = null!;

        public SubscriptionType SubscriptionType { get; set; }

        public DateTime SubscriptionContinueTo { get; set; }

        public PaymentTransactionStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public enum PaymentTransactionStatus
    {
        New = 1,
        Paid = 2,
        Refunded = 3,
        Failed  = 4,
        Cancelled = 5,
    }
}
