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
using Microsoft.Extensions.Options;
using StatsBro.Domain.Config;
public interface IDbConnectionFactory
{
    SqliteConnection GetConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly DatabaseConfig _config;
    private readonly object _lock = new ();
    public SqliteConnection _connection = null!;

    public DbConnectionFactory(IOptions<DatabaseConfig> databaseOptions)
    {
        this._config = databaseOptions.Value;
    }

    public SqliteConnection GetConnection()
    {
        //if (_connection == null)
        //{
        //    lock (this._lock)
        //    {
        //        if(_connection == null)
        //        {
        //            var connectionBuilder = new SqliteConnectionStringBuilder()
        //            {
        //                Password = _config.Password,
        //                DataSource = _config.DataSource,
        //                Pooling = _config.Pooling,
        //            };

        //            this._connection = new SqliteConnection(connectionBuilder.ConnectionString);
        //        }
        //    }
        //}

        //return this._connection;

        var connectionBuilder = new SqliteConnectionStringBuilder()
        {
            Password = _config.Password,
            DataSource = _config.DataSource,
            Pooling = _config.Pooling,
        };

        var connection = new SqliteConnection(connectionBuilder.ConnectionString);
        connection.CreateCollation("NOCASE", (x, y) => string.Compare(x, y, ignoreCase: true));
        return connection;
    }
}
