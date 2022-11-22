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
﻿using System.Text;

namespace StatsBro.Domain.Helpers
{
    public class Queues
    {
        public static byte[] BuildMessageBody(object payload) 
        {
            var serializedPayload = System.Text.Json.JsonSerializer.Serialize(payload);
            var messageBody = Encoding.UTF8.GetBytes(serializedPayload);
            return messageBody;
        }
    }
}
