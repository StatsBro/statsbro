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

namespace StatsBro.Domain.Models
{
    public class PurchaseItem
    {
        public string Name { get; set; } = null!;

        public decimal Price { get; set; }

        public int Quantity { get; set; } = 1;
    }

    public class PurchasePayload
    {
        public PaymentProvider Provider { get; set; }

        public string CardToken { get; set; } = null!;

        public SubscriptionType SubscriptionType { get; set; }

        public Guid UserId { get; set; }
        
        public Guid OrganizationId { get; set; }

        public string ThankYouPageUrl { get; set; } = null!;
        public decimal VatValue { get; set; }
        public decimal TotalAmountNet { get; set; }
        public decimal TotalAmountGross { get; set; }
        public decimal TotalVatAmount { get; set; }
        public string Currency { get; set; } = null!;

        public string? ClientIp { get; set; }
    }
}
