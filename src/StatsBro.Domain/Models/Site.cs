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
﻿using System;
using System.Collections.Generic;
using System.Net;

namespace StatsBro.Domain.Models
{
    public class Site
    {
        public Guid Id { get; set; }

        public string Domain { get; set; } = null!;

        public Guid UserId { get; set; }

        public bool IsScriptLive { get; set; }

        public List<string> PersistQueryParamsList { get; set; } = new List<string>();

        public List<IPAddress> IgnoreIPsList { get; set; } = new List<IPAddress>();

        public DateTime CreatedAt { get; set; }
    }
}
