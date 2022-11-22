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

using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
public interface IBootstrapDb
{
    Task Setup();
}

/// <summary>
/// connectionString: https://www.connectionstrings.com/sqlite/
/// </summary>
public class BootstrapDb : IBootstrapDb
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<BootstrapDb> _logger;

    public BootstrapDb(IDbConnectionFactory dbConnectionFactory, ILogger<BootstrapDb> logger)
    {
        this._dbConnectionFactory = dbConnectionFactory;
        this._logger = logger; 
    }

    public async Task Setup()
    {
        var connection = this._dbConnectionFactory.GetConnection();
        await this.CreateTables(connection);
    }

    private async Task CreateTables(SqliteConnection connection)
    {
        await EnsureSchemaMigration(connection);
        var (currentSchemaVersion, isDirty) = await GetLastAppliedSchemaVersion(connection);
        if (isDirty)
        {
            this._logger.LogError("Cannot run dbmigration, schema is dirty, fix it and change dirty to FALSE.");
            return;
        }

        await ApplyMigrations(connection, currentSchemaVersion);
    }

    private async Task EnsureSchemaMigration(SqliteConnection connection)
    {
        var queryTableExists = "SELECT 1 FROM sqlite_master WHERE type='table' AND name = 'schema_migrations'";
        var doesSchemaMigrationsTableExist = await connection.ExecuteScalarAsync<bool>(queryTableExists);

        if (!doesSchemaMigrationsTableExist)
        {
            var createQuery = @"
            CREATE TABLE schema_migrations(
                version INT,
                dirty INT
            );
            
            INSERT INTO schema_migrationS VALUES(0, 0)";

            await connection.ExecuteAsync(createQuery);
        }        
    }

    private async Task<(int, bool)> GetLastAppliedSchemaVersion(SqliteConnection connection)
    {
        var row = await connection.QuerySingleOrDefaultAsync<SchemaMigrationRow>("SELECT version, dirty FROM schema_migrations;");
        if (row == null)
        {
            return (0, false);
        }

        return (int.Parse(row.Version), row.Dirty);
    }

    private async Task ApplyMigrations(SqliteConnection connection, int currentSchemaVersion)
    {
        var sqlFiles = System.IO.Directory.GetFiles("Database/SQL", "*.sql");

        var version = 0;
        try
        {
            foreach (var sqlFileName in sqlFiles.OrderBy(f => f))
            {
                var file = new FileInfo(sqlFileName);
                var fileVersionStr = file.Name[..3];
                var fileVersion = int.Parse(fileVersionStr);
                if (fileVersion > currentSchemaVersion)
                {
                    version = fileVersion;
                    var fs = file.Open(FileMode.Open, FileAccess.Read);
                    TextReader reader = new StreamReader(fs);
                    var fileContent = await reader.ReadToEndAsync();
                    await connection.ExecuteAsync(fileContent);
                }
            }
        }
        catch (Exception exc)
        {
            this._logger.LogError(exc, "An exception while doing migration: {message}", exc.Message);
            await SetVersion(connection, version, true);
            throw;
        }
           
        await SetVersion(connection, version, false);
    }

    private async Task SetVersion(SqliteConnection connection, int version, bool isDirty)
    {
        if (version == 0)
        {
            return;
        }

        await connection.ExecuteAsync("UPDATE schema_migrations SET version = @Version, dirty = @Dirty", new { Version = version, Dirty = isDirty ? 1 : 0 });
    }

    private class SchemaMigrationRow
    {
        public string Version { get; set; } = "0";

        public bool Dirty { get; set; }
    }
}
