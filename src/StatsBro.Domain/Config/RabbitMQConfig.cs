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
﻿namespace StatsBro.Domain.Config
{
    public class RabbitMQConfig
    {
        public string Host { get; set; } = null!;

        public int Port { get; set; } = 5672;

        public string User { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string QueueName { get; set; } = "events";

        public string ExchangeNameConfigReload { get; set; } = "config_reload";

        public uint PrefetchedSize { get; set; } = 0;

        public ushort PrefetchedCount { get; set; } = 1;

        public bool QosIsGlobal { get; set; } = false;
    }
}