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
    public class ESConfig
    {
        public string Uris { get; set; } = "http://localhost:9200;";

        public string PipelineNameProcessing { get; set; } = "statsbro_request-cleanup";

        public int PipelineVersion { get; set; } = 1;

        public string IndexTemplateName { get; set; } = "statsbro_index_template";

        public string IndexPrefix { get; set; } = "";

        public int IndexTemplateVersion { get; set; } = 1;

        public string LifecyclePolicyName { get; set; } = "statsbro_default_lifecycle-policy";
    }
}
