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
    public class NotificationContent
    {
        public OrderNotification? Order { get; set; }

        public OrderNotificationRefund? Refund { get; set; }

        public DateTime? LocalReceiptDateTime { get; set; }

        public List<NotificationProperty> Properties { get; set; } = new List<NotificationProperty>();

        // when refund
        public string OrderId { get; set; } = null!;

        // when refund
        public string ExtOrderId { get; set; } = null!;
    }

    public class NotificationProperty
    {
        public string Name { get; set; } = null!;

        public string Value { get; set; } = null!;
    }

    public class OrderNotification 
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

        public PayMethodNotification? PayMethod { get; set; } = null!;   
    }

    public class PayMethodNotification 
    {
        public string Type { get; set; } = null!;
    }

    public class OrderNotificationRefund
    {
        public string RefundId { get; set; } = null!;

        public string Amount { get; set; } = null!;

        public string CurrencyCode { get; set; } = null!;

        public string Status { get; set; } = null!;

        public DateTime StatusDateTime { get; set; }

        public string Reason { get; set; } = null!;

        public string ReasonDescription { get; set; } = null!;

        public DateTime RefundDate { get; set; }
    }
}
