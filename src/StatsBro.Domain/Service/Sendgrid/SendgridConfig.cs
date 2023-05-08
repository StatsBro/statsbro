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
using System.Collections.Generic;

namespace StatsBro.Domain.Service.Sendgrid
{
    public class SendgridConfig
    {
        public string ApiKey { get; set; } = null!;

        public string SenderEmail { get; set; } = null!;

        public Dictionary<string, string> Templates { get; set; } = new Dictionary<string, string>();

        public const string TemplateNameWeeklyUpdates = "WeeklyUpdates";        
        public const string TemplateNameLoginByMagicLink = "LoginByMagicLink";
    }
}
