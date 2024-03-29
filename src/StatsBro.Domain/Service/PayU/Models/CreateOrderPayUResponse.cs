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
namespace StatsBro.Domain.Service.PayU.Models
{
    public class CreateOrderPayUResponse
    {
        public string RedirectUri { get; set; } = null!;

        public string OrderId { get; set; } = null!;

        /// <summary>
        /// This should be our PaymentId
        /// </summary>
        public string ExtOrderId { get; set; } = null!;

        public Status Status { get; set; } = null!;

        public PayMethods PayMethods { get; set; } = null!;
    }

    public class Status
    {
        public string StatusCode { get; set; } = null!;

        public string StatusDesc { get; set; } = null!;

        public string Severity { get; set; } = null!;

        //TODO use "public enum PayuResponseStatusCode" if you need to understand the response
    }
}
