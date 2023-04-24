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
using StatsBro.Domain.Models;

namespace StatsBro.Host.Panel.Models.Forms;

public class PaymentFormModel
{
    public string PayUClientId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal PriceNet { get; set; }

    public string Currency { get; set; } = null!;

    public decimal VATValue { get; set; } = 23;

    public SubscriptionType SubscriptionType { get; set; }

    public InvoiceDataFormModel InviceAddressData { get; set; } = null!;

    public decimal TotalVATAmount
    {
        get 
        { 
            var result = this.PriceNet * this.VATValue / 100.00m;

            return Math.Round(result, 2);
        }
    }

    public decimal TotalAmount 
    {
        get 
        {
            return this.PriceNet + this.TotalVATAmount;
        }
    } 
}
