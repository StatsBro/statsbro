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
using Microsoft.Extensions.FileProviders;
using Serilog;
using StatsBro.Domain.Config;
using StatsBro.Host.Data.Services;

System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin());
});

// Add services to the container.
builder.Services.AddControllers();

// Configure the HTTP request pipeline.
if (builder.Environment.IsDevelopment())
{
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Logging.AddSerilog(Log.Logger);
Log.Logger.Information("ENV: {env}", builder.Environment.EnvironmentName);

builder.Services.AddSingleton<IPushEventClient, RabbitMQClient>();
builder.Services.Configure<RabbitMQConfig>(builder.Configuration.GetSection(nameof(RabbitMQConfig)));

builder.Host.UseWindowsService();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.MapControllers();

await app.RunAsync();
