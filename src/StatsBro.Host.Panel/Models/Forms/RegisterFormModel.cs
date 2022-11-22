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
﻿using System.ComponentModel.DataAnnotations;

namespace StatsBro.Host.Panel.Models.Forms
{
    public class RegisterFormModel : FormModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email empty")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Email incorrect")]
        public string Email { get; set; } = null!;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Password empty")]
        [MinLength(3, ErrorMessage = "Password too short")]
        public string Password { get; set; } = null!;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Url empty")]
        [DataType(DataType.Url, ErrorMessage = "Url incorrect")]
        public string SiteUrl { get; set; } = null!;


        // bot prevention - simple but works for most cases
        public string? TrapHidden { get; set; }
        public string? TrapNotVisible { get; set; }
        public long CreatedAt { get; set; }

    }
}
