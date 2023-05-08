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
using System.Linq;

namespace StatsBro.Domain.Models
{
    public class SubscriptionPlanDetails
    {
        public SubscriptionType SubscriptionType { get; private set; }

        public decimal PriceNett { get; private set; }

        public string Currency { get; private set; } = null!;

        public int EventsLimit { get; private set; }

        public int WebsitesLimit { get; private set; }

        public int UsersLimit { get; private set; }

        public bool HasApi { get; private set; }

        public bool CanShare { get; private set; }

        private static readonly List<SubscriptionPlanDetails> _subscriptions =
            new List<SubscriptionPlanDetails>
            {
                new SubscriptionPlanDetails
                {
                    SubscriptionType = SubscriptionType.Personal,
                    PriceNett = 29.00m,
                    Currency = "PLN",
                    EventsLimit = 10000,
                    WebsitesLimit = 1,
                    UsersLimit = 1,
                    HasApi = false,
                    CanShare = false,
                },
                new SubscriptionPlanDetails
                {
                    SubscriptionType = SubscriptionType.Business,
                    PriceNett = 149.00m,
                    Currency = "PLN",
                    EventsLimit = 100000,
                    WebsitesLimit = 50,
                    UsersLimit = 10,
                    HasApi = true,
                    CanShare = true,
                },
                new SubscriptionPlanDetails
                {
                    SubscriptionType = SubscriptionType.Enterprise,
                    PriceNett = 2499.00m,
                    Currency = "PLN",
                    EventsLimit = -1,
                    WebsitesLimit = 100,
                    UsersLimit = int.MaxValue,
                    HasApi = true,
                    CanShare = true,
                },
                new SubscriptionPlanDetails
                {
                    SubscriptionType = SubscriptionType.Trial,
                    PriceNett = 0m,
                    Currency = "PLN",
                    EventsLimit = 100000,
                    WebsitesLimit = 50,
                    UsersLimit = 10,
                    HasApi = true,
                    CanShare = true,
                }
            };

        public static IReadOnlyList<SubscriptionPlanDetails> Subscriptions()
        {
            return _subscriptions;
        }

        public static SubscriptionPlanDetails Subscription(SubscriptionType planType)
        {
            return _subscriptions.FirstOrDefault(x => x.SubscriptionType == planType);
        }
    }

    public enum SubscriptionType
    {
        Trial = 0,

        Personal = 1,

        Business = 2,

        Enterprise = 3,
    }
}
