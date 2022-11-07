namespace StatsBro.Storage.Database;

using Microsoft.Data.Sqlite;
using StatsBro.Domain.Models;
using Dapper;
using StatsBro.Domain.Models.DTO;
using StatsBro.Domain.Models.Exceptions;
using System.Collections.Generic;
using System.Data;

public interface IDbRepository
{
    Task<IList<Site>> GetSitesAsync();
    Task<Guid> SaveSiteAsync(Site site);
    Task<IList<Site>> GetSitesForUserAsync(Guid userId);
    Task<Site> GetSiteAsync(Guid siteId);
    Task<User?> GetUserAsync(string email);
    Task<User?> GetUserAsync(Guid id);
    Task<Guid> CreateUserAsync(User newUser);
    Task<Guid> AssignUserToSiteAsync(Guid userId, Guid siteId);
    Task UpdateUserAsync(User user, IDbTransaction transaction = null!);
    Task UpdateUserPasswordAsync(Guid userId, string passwordHash, string passwordSalt, IDbTransaction transaction = null!);
}

public class DbRepository : IDbRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DbRepository(IDbConnectionFactory connectionFactory)
    {
        this._connectionFactory = connectionFactory;
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
                        us.userId = @userId;", 
           new { userId = userId });

        return this.MapSites(siteDTOs).ToList();
    }

    public async Task<Site> GetSiteAsync(Guid siteId)
    {
        try
        {
            using var c = this.Connection;
            var siteDTO = await c.QuerySingleAsync<SiteDTO>(
                @"SELECT s.id, s.domain, s.persistQueryParams, s.ignoreIPs, s.createdAt, s.updatedAt
                    FROM Sites s
                    WHERE 
                        id = @id;",
                new { id = siteId });

            return this.MapSites(new List<SiteDTO>{ siteDTO }).First();
        }
        catch (InvalidOperationException exc) when (exc.Message == "Sequence contains no elements")
        {
            throw new EntityNotFoundException($"siteId: {siteId}", exc);
        }
    }

    public async Task<Guid> SaveSiteAsync(Site site)
    {
        using var c = this.Connection;
        var id = EnsureId(site.Id);
        await c.ExecuteAsync(
        @"INSERT INTO Sites(id, domain, persistQueryParams, ignoreIPs)
            VALUES(@id, @domain, @persistQueryParams, @ignoreIPs)
                ON CONFLICT(id) DO UPDATE SET
                    domain = @domain, persistQueryParams=@persistQueryParams, ignoreIPs=@ignoreIPs
            COLLATE NOCASE;",
        new
        {
            id = id,
            domain = site.Domain,
            persistQueryParams = string.Join(";", site.PersistQueryParamsList),
            ignoreIPs = string.Join(";", site.IgnoreIPsList),
        });

        return id;
    }

    public async Task<Guid> AssignUserToSiteAsync(Guid userId, Guid siteId)
    {
        using var c = this.Connection;
        var id = NewId();
        await c.ExecuteAsync(
            @"INSERT INTO UserSites(id, userId, siteId)
                VALUES(@id, @userId, @siteId)
                COLLATE NOCASE;",
            new { id = id, userId = userId, siteId = siteId });

        return id;
    }

    public async Task<Guid> CreateUserAsync(User newUser)
    {
        using var c = this.Connection;
        var id = NewId();
        await c.ExecuteAsync(
            @"INSERT INTO Users(id, email, passwordHash, passwordSalt)
                    VALUES(@id, @email, @passwordHash, @passwordSalt)
                COLLATE NOCASE;",
            new { id = id, email = newUser.Email, passwordHash = newUser.PasswordHash, passwordSalt = newUser.PasswordSalt });

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
            IgnoreIPsList = s.IgnoreIPs == null ?
                new List<System.Net.IPAddress>() : s.IgnoreIPs.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(ip => System.Net.IPAddress.Parse(ip)).ToList(),
            PersistQueryParamsList = s.PersistQueryParams == null ?
                new List<string>() : s.PersistQueryParams.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
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

    public static Guid NewId()
    {
        return EnsureId();
    }
}
