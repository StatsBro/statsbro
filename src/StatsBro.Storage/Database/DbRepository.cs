/* Copyright StatsBro.io and/or licensed to StatsBro.io under one
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
﻿namespace StatsBro.Storage.Database;

using Microsoft.Data.Sqlite;
using StatsBro.Domain.Models;
using Dapper;
using StatsBro.Domain.Models.DTO;
using StatsBro.Domain.Models.Exceptions;
using System.Collections.Generic;
using System.Data;
using StatsBro.Domain.Helpers;

public interface IDbRepository
{
    Guid NewId();
    Task<IList<Site>> GetSitesAsync();
    Task<Guid> SaveSiteAsync(Site site, IDbTransaction transaction = null!);
    //Task<IList<Site>> GetSitesForUserAsync(Guid userId);
    Task<IList<Site>> GetSitesForOrganizationAsync(Guid organizationId, Guid userId, IDbTransaction transaction = null!); // TODO: rename to GetSitesForUserAsync and use userid, orgid
    Task<IList<OrganizationUserDTO>> GetOrganizationUsersAsync(Guid organizationId);
    Task<Site> GetSiteAsync(Guid siteId, IDbTransaction transaction = null!);
    Task<Site?> GetSiteAsync(string domain);
    Task<Guid?> SaveReferralAsync(Guid organizationId, string @ref, string? cid);
    Task<Site?> GetSiteByShareIdAsync(string linkShareId);
    Task<OrganizationAddressDTO?> GetOrganizationAddressLatestAsync(Guid organizationId);
    Task<User?> GetUserAsync(string email);
    Task AddOrganizationAddressAsync(OrganizationAddressDTO organizationAddressDTO);
    Task UserLoggedInAsync(Guid userId);
    Task<User?> GetUserAsync(Guid id);
    Task<Guid> CreateUserAsync(User newUser, IDbTransaction transaction = null!);
    Task<Guid> CreateOrganizationAsync(Organization org, IDbTransaction transaction = null!);
    Task AssignUserToOrganizationAsync(Guid userId, Guid organizationId, OrganizationUserRole role, IDbTransaction transaction = null!);
    Task UpdateUserAsync(User user, IDbTransaction transaction = null!);
    Task UpdateUserPasswordAsync(Guid userId, string passwordHash, string passwordSalt, IDbTransaction transaction = null!);
    Task SaveSiteSharingSettingsAsync(SiteSharingSettingsDTO siteSharingSettings);
    Task SaveSiteApiSettingsAsync(SiteApiSettingsDTO siteApiSettings);
    Task<SiteSharingSettingsDTO?> GetSiteSharingSettingsAsync(Guid siteId);
    Task<SiteApiSettingsDTO?> GetSiteApiSettingsAsync(Guid siteId);
    Task<SiteApiSettingsDTO?> GetSiteApiSettingsAsync(string apiKey);
    Task<OrganizationUserDetailsDTO> GetOrganizationForUserIdAsync(Guid userId);
    Task<OrganizationDTO> GetOrganizationAsync(Guid organizationId);
    Task DeleteUserAsync(Guid userId, Guid organizationId);
    Task<Guid> CreatePaymentAsync(PaymentDTO payment, IDbTransaction? transaction = null);
    Task<Guid> CreateCreditCardTokenAsync(CreaditCardTokenDTO token, IDbTransaction? transaction = null);
    Task<IDbTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync(IDbTransaction transaction);
    Task RollbackTransactionAsync(IDbTransaction transaction);
    Task<Guid> CreatePaymentTransactionAsync(PaymentTransactionDTO paymentTransaction, IDbTransaction? transaction = null);
    Task<PaymentDTO> GetPaymentAsync(Guid orgId, Guid paymentId);
    Task<PaymentDTO?> GetPaymentAsync(Guid paymentId, string idFromProvider);
    Task SavePaymentAsync(PaymentDTO payment, IDbTransaction? transaction = null);
    Task SetPaymentTransactionStatusAsync(string paymentId, PaymentTransactionStatus status, IDbTransaction? transaction = null);
    Task<PaymentTransactionDTO> GetPaymentTransactionAsync(string id, IDbTransaction? transaction = null);

    /// <summary>
    /// It also sets isSubscriptionCancelled to FALSE
    /// </summary>
    Task SetOrganizationSubscriptionAsync(string organizationId, SubscriptionType subscriptionType, DateTime subscriptionContinueTo, IDbTransaction? transaction = null);
    Task SetOrganizationSubscriptionCancelledAsync(Guid organizationId, IDbTransaction? transaction = null);
    Task ClearOrganizationCreaditCardTokensAsync(Guid organizationId, IDbTransaction? transaction = null);
    Task<MagicLinkDTO?> GetMagicLinkAsync(Guid linkId);
    Task<Guid> AddMagicLinkAsync(MagicLinkDTO magicLink);
}

public class DbRepository : IDbRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    private static bool wasSqlMapperHandlerAdded = false;

    public DbRepository(IDbConnectionFactory connectionFactory)
    {
        this._connectionFactory = connectionFactory;

        if (!wasSqlMapperHandlerAdded)
        {
            SqlMapper.AddTypeHandler(new GuidHandler()); // register handler to deserialize GUID from db string
            wasSqlMapperHandlerAdded = true;
        }
    }

    public class GuidHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value)
        {
            if (value == null)
            {
                return Guid.Empty;
            }

            var str = value.ToString();
            return Guid.Parse(str!);
        }

        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value;
        }
    }

    public async Task<User?> GetUserAsync(string email)
    {
        try
        {
            using var c = this.Connection;
            var dto = await c.QuerySingleAsync<UserDTO>(
                  @"SELECT id, email, passwordHash, passwordSalt, registeredAt
                    FROM Users
                    WHERE email = @email
                    COLLATE NOCASE;",
                  new { email });

            return new User
            {
                Email = dto.Email,
                Id = Guid.Parse(dto.Id),
                PasswordHash = dto.PasswordHash,
                PasswordSalt = dto.PasswordSalt,
                RegisteredAt = dto.RegisteredAt
            };
        }
        catch (InvalidOperationException exc) when (exc.Message == "Sequence contains no elements")
        {
            return null;
        }
    }

    public async Task UserLoggedInAsync(Guid id)
    {
        using var c = this.Connection;
        await c.ExecuteAsync(
            @"UPDATE Users 
                SET lastLoggedInAt = current_timestamp 
                WHERE id = @id
                COLLATE NOCASE;",
                new { id }
            );
    }

    public async Task<User?> GetUserAsync(Guid id)
    {
        try
        {
            using var c = this.Connection;
            var dto = await c.QuerySingleAsync<UserDTO>(
                  @"SELECT id, email, registeredAt
                    FROM Users
                    WHERE id = @id;",
                  new { id });

            return new User
            {
                Email = dto.Email,
                Id = Guid.Parse(dto.Id),
                RegisteredAt = dto.RegisteredAt,
            };
        }
        catch (InvalidOperationException exc) when (exc.Message == "Sequence contains no elements")
        {
            return null;
        }
    }

    public async Task<IList<Site>> GetSitesAsync()
    {
        using var c = this.Connection;
        var siteDTOs = await c.QueryAsync<SiteDTO>(
            @"SELECT id, domain, persistQueryParams, ignoreIPs, createdAt 
                    FROM Sites;");

        return this.MapSites(siteDTOs).ToList();
    }

    public async Task<IList<Site>> GetSitesForUserAsync(Guid userId)
    {
        using var c = this.Connection;
        var siteDTOs = await c.QueryAsync<SiteDTO>(
           @"SELECT DISTINCT s.id, s.domain,s.persistQueryParams, s.ignoreIPs, s.createdAt, s.updatedAt
                    FROM Sites s
                        INNER JOIN UserSites us ON s.id = us.siteId
                    WHERE 
                        us.userId = @userId
                    COLLATE NOCASE;",
           new { userId = userId });

        return this.MapSites(siteDTOs).ToList();
    }

    public async Task<IList<Site>> GetSitesForOrganizationAsync(Guid organizationId, Guid userId, IDbTransaction transaction = null!)
    {
        using var c = this.GetTheConnection(transaction);
        var siteDTOs = await c.Connection.QueryAsync<SiteDTO>(
           @"SELECT DISTINCT s.id, s.domain,s.persistQueryParams, s.ignoreIPs, s.createdAt, s.updatedAt
                    FROM Sites s
                    WHERE 
                        s.organizationId = @organizationId
                        AND EXISTS(SELECT 1 FROM OrganizationUsers ou WHERE ou.userId = @userId)
                    COLLATE NOCASE;",
           new { organizationId = organizationId, userId = userId });

        return this.MapSites(siteDTOs).ToList();
    }

    public async Task<Site> GetSiteAsync(Guid siteId, IDbTransaction transaction = null!)
    {
        try
        {
            using var c = this.GetTheConnection(transaction);
            var siteDTO = await c.Connection.QuerySingleAsync<SiteDTO>(
                @"SELECT s.id, s.domain, s.persistQueryParams, s.ignoreIPs, s.createdAt, s.updatedAt
                    FROM Sites s
                    WHERE 
                        id = @id
                    COLLATE NOCASE;",
                new { id = siteId });

            return this.MapSites(new List<SiteDTO> { siteDTO }).First();
        }
        catch (InvalidOperationException exc) when (exc.Message == "Sequence contains no elements")
        {
            throw new EntityNotFoundException($"siteId: {siteId}", exc);
        }
    }

    public async Task<Site?> GetSiteAsync(string domain)
    {
        try
        {
            using var c = this.Connection;
            var siteDTOs = await c.QueryAsync<SiteDTO>(
               @"SELECT DISTINCT s.id, s.domain,s.persistQueryParams, s.ignoreIPs, s.createdAt, s.updatedAt
                    FROM Sites s
                    WHERE 
                        s.domain = @domain
                    COLLATE NOCASE;",
               new { domain });

            return this.MapSites(siteDTOs).SingleOrDefault();
        }
        catch (InvalidOperationException exc) when (exc.Message == "Sequence contains no elements")
        {
            return null;
        }
    }

    public async Task<Guid> SaveSiteAsync(Site site, IDbTransaction transaction = null!)
    {
        var id = EnsureId(site.Id);
        using var c = this.GetTheConnection(transaction);
        await c.Connection.ExecuteAsync(
        @"INSERT INTO Sites(id, domain, persistQueryParams, ignoreIPs, organizationId)
            VALUES(@id, @domain, @persistQueryParams, @ignoreIPs, @organizationId)
                ON CONFLICT(id) DO UPDATE SET
                    domain = @domain, persistQueryParams=@persistQueryParams, ignoreIPs=@ignoreIPs
            COLLATE NOCASE;",
        new
        {
            id = id,
            domain = site.Domain,
            persistQueryParams = string.Join(Consts.SiteSettingsQueryParamsSeparator, site.PersistQueryParamsList),
            ignoreIPs = string.Join(Consts.SiteSettingsIPsSeparator, site.IgnoreIPsList),
            organizationId = site.OrganizationId,
        });

        return id;
    }
        
    public async Task AssignUserToOrganizationAsync(Guid userId, Guid organizationId, OrganizationUserRole role, IDbTransaction transaction = null!)
    {
        using var c = this.GetTheConnection(transaction);
        await c.Connection.ExecuteAsync(
            @"INSERT INTO OrganizationUsers(organizationId, userId, role)
                VALUES(@organizationId, @userId, @role);",
            new { organizationId, userId, role });
    }

    public async Task<Guid> CreateUserAsync(User newUser, IDbTransaction transaction = null!)
    {
        using var c = this.GetTheConnection(transaction);
        var id = NewId();
        await c.Connection.ExecuteAsync(
            @"INSERT INTO Users(id, email, passwordHash, passwordSalt)
                    VALUES(@id, @email, @passwordHash, @passwordSalt)
               ;",
            new { id = id, email = newUser.Email, passwordHash = newUser.PasswordHash, passwordSalt = newUser.PasswordSalt });

        return id;
    }

    public async Task<Guid> CreateOrganizationAsync(Organization org, IDbTransaction transaction = null!)
    {
        using var c = this.GetTheConnection(transaction);
        var id = NewId();
        await c.Connection.ExecuteAsync(
            @"INSERT INTO Organizations(id, name, subscriptionType, subscriptionValidTo) 
                VALUES(@id, @name, @subscriptionType, @subscriptionValidTo)
                ;",
            new 
            { 
                id = id, 
                name = org.Name,
                subscriptionType = org.SubscriptionType.ToString(),
                subscriptionValidTo = org.SubscriptionValidTo,
            });

        return id;
    }

    public async Task UpdateUserAsync(User user, IDbTransaction transaction = null!)
    {
        using var c = this.Connection;
        await c.ExecuteAsync(
            @"UPDATE Users
            SET email = @email, updatedAt = datetime('now','utc')
            WHERE id = @userId
            COLLATE NOCASE;",
            new { userId = user.Id, email = user.Email },
            transaction: transaction);
    }

    public async Task DeleteUserAsync(Guid userId, Guid organizationId)
    {
        using var c = this.Connection;
        var affectedRows = await c.ExecuteAsync(
            @"DELETE FROM OrganizationUsers 
            WHERE userId = @userId AND organizationId = @organizationId;
            DELETE FROM Users 
            WHERE id = @userId
            COLLATE NOCASE;",
            new { userId = userId, organizationId = organizationId }
            );

        if (affectedRows != 2)
        {
            throw new Exception($"Removing user failed, affected rows: {affectedRows} but was expecting 2.");
        }
    }

    public async Task UpdateUserPasswordAsync(Guid userId, string passwordHash, string passwordSalt, IDbTransaction transaction = null!)
    {
        using var c = this.Connection;
        await c.ExecuteAsync(
            @"UPDATE Users 
                SET passwordHash = @passwordHash, passwordSalt = @passwordSalt, updatedAt = datetime('now','utc')
                WHERE id = @userId
                COLLATE NOCASE;"
            , new { userId, passwordHash, passwordSalt },
            transaction: transaction);
    }

    private IEnumerable<Site> MapSites(IEnumerable<SiteDTO> siteDTOs)
    {
        return siteDTOs.Select(s => new Site
        {
            Id = Guid.Parse(s.Id),
            Domain = s.Domain,
            OrganizationId = s.OrganizationId != null ? Guid.Parse(s.OrganizationId) : Guid.Empty,
            IgnoreIPsList = s.IgnoreIPs == null ?
                new List<System.Net.IPAddress>() : s.IgnoreIPs.Split(Consts.SiteSettingsIPsSeparator, StringSplitOptions.RemoveEmptyEntries).Select(ip => System.Net.IPAddress.Parse(ip)).ToList(),
            PersistQueryParamsList = s.PersistQueryParams == null ?
                new List<string>() : s.PersistQueryParams.Split(Consts.SiteSettingsQueryParamsSeparator, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToList()
        });
    }

    private SqliteConnection Connection
    {
        get
        {
            return _connectionFactory.GetConnection();
        }
    }

    public static Guid EnsureId(Guid? id = null)
    {
        if (id == null || id == Guid.Empty)
        {
            return Guid.NewGuid();
        }

        return id.Value;
    }

    public static Guid EnsureId(string? id = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Guid.NewGuid();
        }

        return Guid.Parse(id);
    }

    public Guid NewId()
    {
        return EnsureId("");
    }

    public async Task SaveSiteSharingSettingsAsync(SiteSharingSettingsDTO siteSharingSettings)
    {
        using var c = this.Connection;

        await c.ExecuteAsync(
        @"INSERT INTO SiteSharingSettings(siteId, linkShareId)
            VALUES(@siteId, @linkShareId)
                ON CONFLICT(siteId) DO UPDATE SET
                    linkShareId=@linkShareId
            COLLATE NOCASE;",
        new
        {
            siteId = siteSharingSettings.SiteId,
            linkShareId = siteSharingSettings.LinkShareId,
        });
    }

    public async Task SaveSiteApiSettingsAsync(SiteApiSettingsDTO siteApiSettings)
    {
        using var c = this.Connection;

        await c.ExecuteAsync(
        @"INSERT INTO SiteApiSettings(siteId, apiKey)
            VALUES(@siteId, @apiKey)
                ON CONFLICT(siteId) DO UPDATE SET
                    apiKey=@apiKey
            COLLATE NOCASE;",
        new
        {
            siteId = siteApiSettings.SiteId,
            apiKey = siteApiSettings.ApiKey,
        });
    }

    public async Task<SiteSharingSettingsDTO?> GetSiteSharingSettingsAsync(Guid siteId)
    {
        try
        {
            using var c = this.Connection;
            var result = await c.QuerySingleAsync<SiteSharingSettingsDTO>(
               @"SELECT siteId, linkShareId
                    FROM SiteSharingSettings
                    WHERE siteId = @siteId
                COLLATE NOCASE;",
               new { siteId = siteId });

            return result;
        }
        catch (InvalidOperationException exc) when (exc.Message == "Sequence contains no elements")
        {
            return null;
        }
    }

    public async Task<SiteApiSettingsDTO?> GetSiteApiSettingsAsync(Guid siteId)
    {
        try
        {
            using var c = this.Connection;
            var result = await c.QuerySingleAsync<SiteApiSettingsDTO>(
               @"SELECT siteId, apiKey
                    FROM SiteApiSettings
                    WHERE siteId = @siteId
                COLLATE NOCASE;",
               new { siteId = siteId });

            return result;
        }
        catch (InvalidOperationException exc) when (exc.Message == "Sequence contains no elements")
        {
            return null;
        }
    }

    public async Task<SiteApiSettingsDTO?> GetSiteApiSettingsAsync(string apiKey)
    {
        try
        {
            using var c = this.Connection;
            var result = await c.QuerySingleAsync<SiteApiSettingsDTO>(
               @"SELECT siteId, apiKey
                    FROM SiteApiSettings
                    WHERE apiKey = @apiKey
                COLLATE NOCASE;",
               new { apiKey = apiKey });

            return result;
        }
        catch (InvalidOperationException exc) when (exc.Message == "Sequence contains no elements")
        {
            return null;
        }
    }

    public async Task<Site?> GetSiteByShareIdAsync(string linkShareId)
    {
        try
        {
            using var c = this.Connection;
            var siteDTOs = await c.QueryAsync<SiteDTO>(
               @"SELECT DISTINCT s.id, s.domain,s.persistQueryParams, s.ignoreIPs, s.createdAt, s.updatedAt, s.organizationId
                    FROM Sites s
                    INNER JOIN SiteSharingSettings sss ON s.id = sss.siteId
                    WHERE 
                        sss.linkShareId = @linkShareId
                    COLLATE NOCASE;",
               new { linkShareId });

            return this.MapSites(siteDTOs).SingleOrDefault();
        }
        catch (InvalidOperationException exc) when (exc.Message == "Sequence contains no elements")
        {
            return null;
        }
    }

    public async Task<OrganizationUserDetailsDTO> GetOrganizationForUserIdAsync(Guid userId)
    {
        // assumption now is that user has only one organization
        using var c = this.Connection;
        var queryResult = await c.QuerySingleAsync<OrganizationUserWithUserQueryResult>(
            @"SELECT 
                ou.organizationId, ou.userId, ou.role userRole,
                o.name AS orgName, o.createdAt AS orgCreatedAt
             FROM 
                OrganizationUsers ou 
                INNER JOIN Organizations o on ou.organizationId = o.id
             WHERE 
                ou.userId = @userId
            COLLATE NOCASE;",
            new { userId });

        return new OrganizationUserDetailsDTO
        {
            Organization = new OrganizationDTO
            {
                CreatedAt = queryResult.OrgCreateAt,
                Id = Guid.Parse(queryResult.OrganizationId),
                Name = queryResult.OrgName,
            },
            Role = queryResult.UserRole,
            UserId = Guid.Parse(queryResult.UserId),
        };
    }

    public async Task<OrganizationDTO> GetOrganizationAsync(Guid id)
    {
        using var c = this.Connection;
        var queryResult = await c.QuerySingleAsync<OrganizationDTO>(
            @"SELECT
                id, name, createdAt, updatedAt, subscriptionType, subscriptionValidTo, isSubscriptionCancelled
            FROM
                Organizations
            WHERE
                id = @id
            COLLATE NOCASE;",
            new { id = id });

        return queryResult;
    }

    public async Task<IList<OrganizationUserDTO>> GetOrganizationUsersAsync(Guid organizationId)
    {
        using var c = this.Connection;
        var queryResult = await c.QueryAsync<OrganizationUserWithUserQueryResult>(
            @"SELECT 
                ou.organizationId, ou.userId, ou.role userRole,
                u.email userEmail, u.registeredAt userRegisteredAt, u.lastLoggedInAt userLastLoggedInAt
             FROM 
                OrganizationUsers ou 
                INNER JOIN Users u on ou.userId = u.id
             WHERE 
                ou.organizationId = @organizationId
            COLLATE NOCASE;",
            new { organizationId });

        return queryResult.Select(x =>
            new OrganizationUserDTO {
                OrganizationId = organizationId,
                Role = x.UserRole,
                UserDTO = new UserDTO {
                    Id = x.UserId,
                    Email = x.UserEmail,
                    LastLoggedInAt = x.UserLastLoggedInAt,
                    RegisteredAt = x.UserRegisteredAt,
                } })
            .ToList();
    }

    public async Task<Guid> CreatePaymentAsync(PaymentDTO payment, IDbTransaction? transaction = null)
    {
        var id = EnsureId(payment.Id);
        using var c = this.GetTheConnection(transaction);
        await c.Connection.ExecuteAsync(
           @"INSERT INTO Payments(id, idFromProvider, provider, organizationId, userId, status, source)
                VALUES(@id, @idFromProvider, @provider, @organizationId, @userId, @status, @source)
                ;",
           new
           {
               id = id,
               idFromProvider = payment.IdFromProvider,
               provider = payment.Provider.ToString(),
               organizationId = payment.OrganizationId.ToUpperInvariant(),
               userId = payment.UserId.ToUpperInvariant(),
               status = payment.Status.ToString(),
               source = payment.Source.ToString(),
           });

        return id;
    }

    public async Task<Guid> CreateCreditCardTokenAsync(CreaditCardTokenDTO token, IDbTransaction? transaction = null)
    {
        var id = EnsureId(token.Id);
        using var c = this.GetTheConnection(transaction);
        await c.Connection.ExecuteAsync(
           @"INSERT INTO CreditCardTokens(id, organizationId, userId, provider, token, cardNumberMasked, cardExpirationMonth, cardExpirationYear)
                VALUES(@id, @organizationId, @userId, @provider, @token, @cardNumberMasked, @cardExpirationMonth, @cardExpirationYear)
                ;",
           new
           {
               id = id,
               organizationId = token.OrganizationId,
               userId = token.UserId,
               provider = token.Provider.ToString(),
               token = token.Token,
               cardNumberMasked = token.CardNumberMasked,
               cardExpirationMonth = token.CardExpirationMonth,
               cardExpirationYear = token.CardExpirationYear,
           });

        return id;
    }

    public async Task<Guid> CreatePaymentTransactionAsync(PaymentTransactionDTO paymentTransaction, IDbTransaction? transaction = null)
    {
        var id = EnsureId(paymentTransaction.Id);
        using var c = this.GetTheConnection(transaction);
        await c.Connection.ExecuteAsync(
            @"INSERT INTO PaymentTransactions(id, organizationId, paymentId, amountNet, amountGross, vatValue,vatAmount, currency, subscriptionType, subscriptionContinueTo)
                VALUES(@id, @organizationId, @paymentId, @amountNet, @amountGross, @vatValue, @vatAmount, @currency, @subscriptionType, @subscriptionContinueTo)",
            new
            {
                id = id,
                organizationId = paymentTransaction.OrganizationId.ToUpperInvariant(),
                paymentId = paymentTransaction.PaymentId.ToUpperInvariant(),
                amountNet = paymentTransaction.AmountNet,
                amountGross = paymentTransaction.AmountGross,
                vatValue = paymentTransaction.VatValue,
                vatAmount = paymentTransaction.VatAmount,
                currency = paymentTransaction.Currency,
                subscriptionType = paymentTransaction.SubscriptionType.ToString(),
                subscriptionContinueTo = paymentTransaction.SubscriptionContinueTo,
            });

        return id;
    }

    public async Task<PaymentDTO> GetPaymentAsync(Guid orgId, Guid paymentId)
    {
        using var c = this.Connection;
        var payment = await c.QueryFirstOrDefaultAsync<PaymentDTO>(
            @"SELECT id, idFromProvider, provider, organizationId, userId, status, source, createdAt, updatedAt
            FROM Payments
            WHERE id = @id AND organizationId = @organizationId
            COLLATE NOCASE;",
            new {
                id = paymentId,
                organizationId = orgId,
            });

        return payment;
    }

    public async Task<PaymentDTO?> GetPaymentAsync(Guid paymentId, string idFromProvider)
    {
        using var c = this.Connection;
        var payment = await c.QueryFirstOrDefaultAsync<PaymentDTO>(
            @"SELECT id, idFromProvider, provider, organizationId, userId, status, source, createdAt, updatedAt
            FROM 
                Payments
            WHERE 
                id = @id AND idFromProvider = @idFromProvider
            COLLATE NOCASE;",
            new
            {
                id = paymentId,
                idFromProvider = idFromProvider,
            });

        return payment;
    }

    public async Task SavePaymentAsync(PaymentDTO payment, IDbTransaction? transaction = null)
    {
        using var c = this.GetTheConnection(transaction);
        var updatedCount = await c.Connection.ExecuteAsync(
            @"UPDATE 
                Payments
             SET 
                 status = @status
                 ,updatedAt = @updatedAt
            WHERE 
                id = @id
             COLLATE NOCASE;",
            new
            {
                id = payment.Id.ToUpperInvariant(),
                status = payment.Status.ToString(),
                updatedAt = DateTime.UtcNow,
            });

        if (updatedCount == 0)
        {
            throw new Exception($"Nothing was updated, but should update payment: {payment.Id}");
        }
    }

    public async Task SetPaymentTransactionStatusAsync(string paymentId, PaymentTransactionStatus status, IDbTransaction? transaction = null)
    {
        using var c = this.GetTheConnection(transaction);
        var updatedCount = await c.Connection.ExecuteAsync(
            @"UPDATE 
                PaymentTransactions
             SET 
                 status = @status
                 ,updatedAt = @updatedAt
            WHERE 
                paymentId = @paymentId
             COLLATE NOCASE;",
            new
            {
                paymentId = paymentId,
                status = status.ToString(),
                updatedAt = DateTime.UtcNow,
            });

        if (updatedCount == 0)
        {
            throw new Exception($"Nothing was updated, but should update paymentTransaction for payment: {paymentId}");
        }
    }

    public async Task<PaymentTransactionDTO> GetPaymentTransactionAsync(string paymentId, IDbTransaction? transaction = null)
    {
        using var c = this.GetTheConnection(transaction);
        var paymentTransaction = await c.Connection.QuerySingleAsync<PaymentTransactionDTO>(
            @"SELECT
                id, organizationId, paymentId, amountNet, amountGross, vatValue, vatAmount, currency, subscriptionType, subscriptionContinueTo, createdAt, status, updatedAt
            FROM
                PaymentTransactions
            WHERE
                paymentId = @paymentId
            COLLATE NOCASE;",
            new
            {
                paymentId = paymentId
            });

        return paymentTransaction;
    }

    /// <summary>
    /// It also sets isSubscriptionCancelled to FALSE
    /// </summary>
    /// <exception cref="Exception">throws Exception when data was not uppdated</exception>
    public async Task SetOrganizationSubscriptionAsync(string organizationId, SubscriptionType subscriptionType, DateTime subscriptionContinueTo, IDbTransaction? transaction)
    {
        using var c = this.GetTheConnection(transaction);
        var updatedCount = await c.Connection.ExecuteAsync(
            @"UPDATE 
                Organizations
             SET 
                 subscriptionType = @subscriptionType
                 ,subscriptionValidTo = @subscriptionValidTo
                 ,updatedAt = @updatedAt
                 ,isSubscriptionCancelled = 0
            WHERE 
                id = @organizationId
             COLLATE NOCASE;",
            new
            {
                organizationId = organizationId,
                subscriptionType = subscriptionType.ToString(),
                subscriptionValidTo = subscriptionContinueTo,
                updatedAt = DateTime.UtcNow,
            });

        if (updatedCount == 0)
        {
            throw new Exception($"Nothing was updated, but should update organization subscription data with organizationId: {organizationId}, subscriptionType: {subscriptionType}, subscriptionContinueTo: {subscriptionContinueTo}");
        }
    }

    public async Task SetOrganizationSubscriptionCancelledAsync(Guid organizationId, IDbTransaction? transaction = null)
    {
        using var c = this.GetTheConnection(transaction);
        var updatedCount = await c.Connection.ExecuteAsync(
            @"UPDATE 
                Organizations
             SET 
                 isSubscriptionCancelled = 1
                 ,updatedAt = @updatedAt
            WHERE 
                id = @organizationId
             COLLATE NOCASE;",
            new
            {
                organizationId = organizationId,
                updatedAt = DateTime.UtcNow,
            });

        if (updatedCount == 0)
        {
            throw new Exception($"Nothing was updated, but should update organization isSubscriptionCancelled to TRUE for organizationId: {organizationId}");
        }
    }

    public async Task ClearOrganizationCreaditCardTokensAsync(Guid organizationId, IDbTransaction? transaction = null)
    {
        using var c = this.GetTheConnection(transaction);
        var updatedCount = await c.Connection.ExecuteAsync(
            @"UPDATE 
                CreditCardTokens
             SET 
                 token = ''
                 ,cardNumbermasked = ''
                 ,cardExpirationMonth = 1
                 ,cardExpirationyear = 2000
            WHERE 
                organizationId = @organizationId
            COLLATE NOCASE;",
            new
            {
                organizationId = organizationId,                
            });
    }

    public async Task<OrganizationAddressDTO?> GetOrganizationAddressLatestAsync(Guid organizationId)
    {
        try
        { 
            using var c = this.Connection;
            var orgAddress = await c.QueryFirstAsync<OrganizationAddressDTO>(
               @"SELECT
                    organizationId, name, addressLine1, postalCode, city, nip, createdAt
                FROM
                    OrganizationAddresses
                WHERE
                    organizationId = @organizationId
                    COLLATE NOCASE
                ORDER BY 
                    createdAt DESC
               ;",
               new
               {
                   organizationId = organizationId
               });

            return orgAddress;
        }
        catch (InvalidOperationException exc) when (exc.Message == "Sequence contains no elements")
        {
            return null;
        }
    }

    public async Task AddOrganizationAddressAsync(OrganizationAddressDTO orgAddress)
    {
        using var c = this.Connection;
        await c.ExecuteAsync(
            @"INSERT INTO 
                OrganizationAddresses(organizationId, name, addressLine1, postalCode, city, nip)
             VALUES(@organizationId, @name, @addressLine1, @postalCode, @city, @nip)
        ",
            new {
                organizationId = orgAddress.OrganizationId,
                name = orgAddress.Name,
                addressLine1 = orgAddress.AddressLine1,
                postalCode = orgAddress.PostalCode,
                city = orgAddress.City,
                nip = orgAddress.NIP,
            });
    }

    public async Task<Guid?> SaveReferralAsync(Guid organizationId, string @ref, string? cid)
    {
        var id = EnsureId((string?)null);
        using var c = this.Connection;
        var updatedCount = await c.ExecuteAsync(
            @"INSERT INTO  
                Referrals(id, organizationId, referralKey, cid)
             VALUES(@id, @organizationId, @referralKey, @cid);",
            new
            {
                id = id,
                organizationId = organizationId,
                referralKey = @ref,
                cid = cid
            });

        return updatedCount > 0 ? id : null;
    }

    public async Task<MagicLinkDTO?> GetMagicLinkAsync(Guid linkId)
    {
        try
        {
            using var c = this.Connection;
            var magicLink = await c.QueryFirstAsync<MagicLinkDTO>(
               @"SELECT
                    id, validTo, userId, origin, createdAt
                FROM
                    MagicLinks
                WHERE
                    id = @id
                    COLLATE NOCASE
               ;",
               new
               {
                   id = linkId,
               });

            return magicLink;
        }
        catch (InvalidOperationException exc) when (exc.Message == "Sequence contains no elements")
        {
            return null;
        }
    }

    public async Task<Guid> AddMagicLinkAsync(MagicLinkDTO magicLink)
    {
        magicLink.Id = EnsureId(magicLink.Id);

        using var c = this.Connection;
        await c.ExecuteAsync(
            @"INSERT INTO 
                MagicLinks(id, validTo, userId, origin)
             VALUES(@id, @validTo, @userId, @origin)
            ",
            new
            {
                id = magicLink.Id,
                validTo = magicLink.ValidTo,
                userId = magicLink.UserId,
                origin = magicLink.Origin.ToString(),
            });

        return magicLink.Id;
    }

    public async Task<IDbTransaction> BeginTransactionAsync()
    {
        var c = this.Connection;
        try
        {
            await c.OpenAsync();
            return await c.BeginTransactionAsync();
        }
        catch (Exception)
        {
            c.Dispose();
            throw;
        }
    }

    public async Task CommitTransactionAsync(IDbTransaction transaction)
    {
        try
        {
            if (transaction is not SqliteTransaction t)
            {
                transaction.Commit();
                return;
            }

            await t.CommitAsync();
        }
        finally
        {
            transaction.Dispose();
        }
    }

    public async Task RollbackTransactionAsync(IDbTransaction transaction)
    {
        try
        {
            if (transaction is not SqliteTransaction t)
            {
                transaction.Rollback();
                return;
            }

            await t.RollbackAsync();
        }
        finally 
        { 
            transaction.Dispose(); 
        }
    }

    private class OrganizationUserWithUserQueryResult
    {
        public string OrganizationId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string OrgName { get; set; } = null!;
        public DateTime OrgCreateAt { get; set; }
        public string UserEmail { get; set; } = null!;
        public OrganizationUserRole UserRole { get; set; }
        public DateTime UserRegisteredAt { get; set; }
        public DateTime? UserLastLoggedInAt { get; set; }
    }

    private class TheConnection : IDisposable
    {
        private readonly bool _isTransaction = false;
        private readonly IDbTransaction? _transaction;
        private readonly Func<SqliteConnection> _fnGetConnection;
        private SqliteConnection? _connection = null!;
        private bool _isDisposed;

        public TheConnection(IDbTransaction? transaction, Func<SqliteConnection> fnGetConnection)
        {
            _transaction = transaction;
            _isTransaction = transaction != null;
            _fnGetConnection = fnGetConnection;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IDbConnection Connection
        {
            get
            {
                if (_transaction != null && _transaction.Connection != null)
                {
                    return _transaction.Connection;
                }

                _connection = _fnGetConnection();
                return _connection;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                // free managed resources
                if (_connection != null)
                {
                    _connection.Dispose();
                }
            }

            _isDisposed = true;
        }
    }

    private TheConnection GetTheConnection(IDbTransaction? transaction)
    {
        return new TheConnection(transaction, () => this.Connection);
    }
}
