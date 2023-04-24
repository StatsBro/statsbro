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
using System.Collections.Generic;

namespace StatsBro.Domain.Service.PayU.Models
{
    public class GetOrderResponse
    {
        public List<Order> Orders { get; set; } = null!;

        public Status Status { get; set; } = null!;
    }

    public class Order
    {
        public string OrderId { get; set; } = null!;

        public string ExtOrderId { get; set; } = null!;

        public DateTime OrderCreatedDate { get; set; }

        public string NotifyUrl { get; set; } = null!;

        public string CustomerIp { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string CurrencyCode { get; set; } = null!;

        public string TotalAmount { get; set; } = null!;

        public string Status { get; set; } = null!;

        public DateTime LocalReceiptDateTime { get; set; }
    }
}
