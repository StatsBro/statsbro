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
using StatsBro.Domain.Models.DTO;
using StatsBro.Host.Panel.Models.Forms;
using StatsBro.Storage.Database;

namespace StatsBro.Host.Panel.Logic;

public class OrganizationLogic
{
    private readonly IDbRepository _repository;

    public OrganizationLogic(IDbRepository repository)
    {
        this._repository = repository;
    }

    public async Task<IList<OrganizationUserDTO>> GetOrganizationUsersAsync(Guid organizationId)
    {
        var users = await _repository.GetOrganizationUsersAsync(organizationId);
        return users;
    }

    public async Task<OrganizationDTO> GetOrganizationAsync(Guid organizationId)
    {
        return await _repository.GetOrganizationAsync(organizationId);
    }

    public async Task SaveOrganizationAddressAsync(Guid organizationId, InvoiceDataFormModel model)
    {
        Func<InvoiceDataFormModel, OrganizationAddressDTO, bool> areEqual = (m, a) =>
        {
            return m.NIP == a.NIP &&
            m.Name == a.Name &&
            m.PostalCode == a.PostalCode &&
            m.AddressLine1 == a.AddressLine1 &&
            m.City == a.City;
        };

        var currentAddress = await _repository.GetOrganizationAddressLatestAsync(organizationId);
        if (currentAddress != null && areEqual(model, currentAddress)) {
            return;
        }

        await _repository.AddOrganizationAddressAsync(new OrganizationAddressDTO {
            OrganizationId = organizationId,
            City = model.City,
            Name = model.Name,
            PostalCode = model.PostalCode,
            AddressLine1 = model.AddressLine1,
            NIP = model.NIP,
        });
    }

    public async Task<OrganizationAddressDTO?> GetOrganizationAddressAsync(Guid organizationId)
    {
        return await _repository.GetOrganizationAddressLatestAsync(organizationId);
    }
}
