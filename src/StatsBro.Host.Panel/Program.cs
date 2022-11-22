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
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Serilog;
using StatsBro.Domain.Config;
using StatsBro.Host.Panel.Logic;
using StatsBro.Host.Panel.Services;
using StatsBro.Storage.Database;
using StatsBro.Storage.ElasticSearch;
using System.Globalization;

System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromHours(4);
            options.SlidingExpiration = true;
            options.AccessDeniedPath = "/Forbidden/";
            options.LoginPath = "/Login";
            options.LogoutPath = "/Logout";
        });

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Logging.AddSerilog(Log.Logger);
Log.Logger.Information("ENV: {env}", builder.Environment.EnvironmentName);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddScoped<UserLogic>();
builder.Services.AddScoped<SiteLogic>();
builder.Services.AddScoped<StatsLogic>();

builder.Services.AddTransient<IDbRepository, DbRepository>();
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

builder.Services.AddSingleton<IEsFactory, EsFactory>();
builder.Services.AddTransient<IEsRepository, EsRepository>();
builder.Services.AddTransient<IEsStatsRepository, EsStatsRepository>();

builder.Services.AddTransient<IMessagingService, MessagingService>();
builder.Services.AddTransient<INotificationService, NotificationService>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddMvc()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new List<CultureInfo>
                    {
                        new CultureInfo("en-US"),
                        new CultureInfo("pl")
                    };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection(nameof(DatabaseConfig)));
builder.Services.Configure<ESConfig>(builder.Configuration.GetSection(nameof(ESConfig)));
builder.Services.Configure<RabbitMQConfig>(builder.Configuration.GetSection(nameof(RabbitMQConfig)));
builder.Services.Configure<SlackConfig>(builder.Configuration.GetSection(nameof(SlackConfig)));

builder.Host.UseWindowsService();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// TODO: global exception handler:
// https://dev.to/moe23/net-6-web-api-global-exceptions-handling-1a46
// https://jasonwatmore.com/post/2022/01/17/net-6-global-error-handler-tutorial-with-example

var supportedCultures = new[]
           {
                new CultureInfo("en-US"),
                new CultureInfo("pl"),
            };

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    // Formatting numbers, dates, etc.
    SupportedCultures = supportedCultures,
    // UI strings that we have localized.
    SupportedUICultures = supportedCultures
});


app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseRequestLocalization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
