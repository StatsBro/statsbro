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
namespace StatsBro.Host.Panel.Models.Forms;

using System.ComponentModel.DataAnnotations;

public class OrganizationUserFormModel : FormModel
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Podaj adres email")]
    [DataType(DataType.EmailAddress, ErrorMessage = "Adres email ma nieprawidłowy format")]
    [EmailAddress(ErrorMessage = "Adres email ma nieprawidłowy format")]
    [MaxLength(512, ErrorMessage = "Adres email jest za długi")]
    public string Email { get; set; } = null!;

    public string? Password { get; set; } = null!;

    public Guid? Id { get; set; } = null!;

    public Guid OrganizationId { get; set; }
}
