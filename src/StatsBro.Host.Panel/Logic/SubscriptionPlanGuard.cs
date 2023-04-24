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
namespace StatsBro.Host.Panel.Logic;

using StatsBro.Domain.Models;
using StatsBro.Domain.Models.DTO;
using StatsBro.Storage.Database;
using System.Collections.Generic;

public interface ISubscriptionPlanGuard
{
    bool CanAddMoreDomains(OrganizationDTO organization, int currentWebsitesCount);
    Task<bool> CanAddMoreUsersAsync(Guid organizationId, int currentUsercCount);
    bool CanAddMoreUsers(OrganizationDTO organization, int currentUsercCount);
    Task<bool> CanAddMoreUsersAsync(Guid organizationId);
    Task<bool> CanAddMoreDomainsAsync(Guid organizationId);
    bool IsSubscriptionPlanExpired(OrganizationDTO organization);
    Task<IReadOnlyList<SubscriptionType>> GetEligibleSubscriptionPlansAsync(Guid organizationId, Guid userId);
}

public class SubscriptionPlanGuard : ISubscriptionPlanGuard
{
    private readonly IDbRepository _repository;

    public SubscriptionPlanGuard(IDbRepository repository)
    {
        _repository = repository;
    }

    public bool CanAddMoreDomains(OrganizationDTO organization, int currentWebsitesCount)
    {
        var s = SubscriptionPlanDetails.Subscription(organization.SubscriptionType);

        return currentWebsitesCount < s.WebsitesLimit;
    }

    public async Task<bool> CanAddMoreDomainsAsync(Guid organizationId)
    {
        var s = await this.GetSubscriptionPlanDetailsAsync(organizationId);
        var users = await _repository.GetOrganizationUsersAsync(organizationId);

        return  users.Count < s.UsersLimit;
    }

    public async Task<bool> CanAddMoreUsersAsync(Guid organizationId, int currentUsercCount)
    {
        var s = await this.GetSubscriptionPlanDetailsAsync(organizationId);

        return currentUsercCount < s.UsersLimit;
    }

    public async Task<bool> CanAddMoreUsersAsync(Guid organizationId)
    {
        var users = await _repository.GetOrganizationUsersAsync(organizationId);
        var s = await this.GetSubscriptionPlanDetailsAsync(organizationId);

        return  users.Count < s.UsersLimit;
    }

    public bool CanAddMoreUsers(OrganizationDTO organization, int currentUsercCount)
    {
        var s = SubscriptionPlanDetails.Subscription(organization.SubscriptionType);

        return currentUsercCount < s.UsersLimit;
    }

    private async Task<SubscriptionPlanDetails> GetSubscriptionPlanDetailsAsync(Guid organizationId)
    {
        var organization = await _repository.GetOrganizationAsync(organizationId);
        return SubscriptionPlanDetails.Subscription(organization.SubscriptionType);
    }

    public bool IsSubscriptionPlanExpired(OrganizationDTO organization)
    {
        var endOfToday = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

        return organization.SubscriptionValidTo > endOfToday;
    }

    public async Task<IReadOnlyList<SubscriptionType>> GetEligibleSubscriptionPlansAsync(Guid organizationId, Guid userId)
    {
        var sites = await _repository.GetSitesForOrganizationAsync(organizationId, userId);
        var users = await _repository.GetOrganizationUsersAsync(organizationId);

        var sitesCount = sites.Count;
        var usersCount = users.Count;

        var result = new List<SubscriptionType>();
        foreach (var st in Enum.GetValues(typeof(SubscriptionType)).Cast<SubscriptionType>())
        {
            var details = SubscriptionPlanDetails.Subscription(st);
            if(sitesCount <= details.WebsitesLimit && usersCount <= details.UsersLimit)
            {
                result.Add(st);
            }
        }

        return result;
    }
}
