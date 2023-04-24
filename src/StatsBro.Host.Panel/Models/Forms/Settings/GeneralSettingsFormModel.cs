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
using System.ComponentModel.DataAnnotations;

namespace StatsBro.Host.Panel.Models.Forms.Settings
{
    public class GeneralSettingsFormModel : FormModel
    {
        public Guid Id { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Podaj adres twojej strony")]
        [DataType(DataType.Url, ErrorMessage = "Adres twojej strony ma nieprawidłowy format")]
        public string SiteUrl { get; set; } = null!;

        public string? Domain { get; set; }

        public string? PersistQueryParamsList { get; set; }
        public string? IgnoreIPsList { get; set; }
    }
}
