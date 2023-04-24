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
using System.Collections.Generic;

namespace StatsBro.Domain.Service.PayU.Models
{
    public class CreateOrderRequest
    {
        /// <summary>
        /// can be null
        /// </summary>
        public string? NotifyUrl { get; set; }

        /// <summary>
        /// can be null
        /// </summary>
        public string ContinueUrl { get; set; } = null!;

        /// <summary>
        /// MUST NOT be null
        /// default: 127.0.0.1
        /// </summary>
        public string CustomerIp { get; set; } = "127.0.0.1";

        /// <summary>
        /// MUST NOT be null
        /// </summary>
        public string MerchantPosId { get; set; } = null!;

        /// <summary>
        /// MUST NOT be null
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// MUST NOT be null
        /// default: PLN
        /// </summary>
        public string CurrencyCode { get; set; } = "PLN";

        /// <summary>
        /// MUST NOT be null
        /// </summary>
        public string TotalAmount { get; set; } = null!;

        /// <summary>
        /// Our id
        /// </summary>
        public string ExtOrderId { get; set; } = null!;

        /// <summary>
        /// can be null
        /// </summary>
        public CreateOrderBuyer Buyer { get; set; } = new CreateOrderBuyer();

        /// <summary>
        /// At least 1 item must be provided
        /// </summary>
        public List<CreateOrderProduct> Products { get; set; } = new List<CreateOrderProduct>();

        /// <summary>
        /// FIRST, STANDARD
        /// </summary>
        public string Recurring { get; set; } = null!;

        public PayMethods PayMethods { get; set; } = null!;
    }

    public class PayMethods
    {
        public PayMethod PayMethod { get; set; } = null!;
    }

    public class PayMethod
    {
        public PayMethodCard Card { get; set; } = null!;

        public string Value { get; set; } = null!;

        /// <summary>
        /// CARD_TOKEN
        /// </summary>
        public string Type { get; set; } = "CARD_TOKEN";
    }

    public class PayMethodCard
    {
        public string Number { get; set; } = null!;
        
        public int ExpirationMonth { get; set; }

        public int ExpirationYear { get; set; }
    }

    public class CreateOrderBuyer
    {
        /// <summary>
        /// can be null
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// can be null
        /// </summary>
        public string Phone { get; set; } = null!;

        /// <summary>
        /// can be null
        /// </summary>
        public string FirstName { get; set; } = null!;

        /// <summary>
        /// can be null
        /// </summary>
        public string LastName { get; set; } = null!;

        /// <summary>
        /// can be null
        /// default: pl
        /// </summary>
        public string Language { get; set; } = "pl";

        public CreateOrderBuyerDelivery Delivery { get; set; } = null!;
    }

    public class CreateOrderBuyerDelivery
    {
        public string Street { get; set; } = null!;

        public string PostalCode { get; set; } = null!;

        public string City { get; set; } = null!;
    }

    public class CreateOrderProduct
    {
        public CreateOrderProduct(string name, string unitPrice, string quantity = "1")
        {
            Name = name;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }

        /// <summary>
        /// MUST NOT be null
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// MUST NOT be null
        /// </summary>
        public string UnitPrice { get; }

        /// <summary>
        /// MUST NOT be null
        /// </summary>
        public string Quantity { get; } = "1";
    }
}
