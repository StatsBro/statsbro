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
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Serilog;
using StatsBro.Domain.Config;
using StatsBro.Host.Panel.Infrastructure.AuthHandlers.HeaderToken;
using StatsBro.Host.Panel.Logic;
using StatsBro.Host.Panel.Services;
using StatsBro.Storage.Database;
using StatsBro.Storage.ElasticSearch;
using System.Globalization;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using StatsBro.Domain.Service.PayU;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.HttpOverrides;
using StatsBro.Domain.Service.Sendgrid;

System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Logging.AddSerilog(Log.Logger);
Log.Logger.Information("ENV: {env}", builder.Environment.EnvironmentName);

var supportedCultures = new List<CultureInfo>
                    {
                        new CultureInfo("en-US"),
                        new CultureInfo("pl")
                    };

builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection(nameof(DatabaseConfig)));
builder.Services.Configure<ESConfig>(builder.Configuration.GetSection(nameof(ESConfig)));
builder.Services.Configure<RabbitMQConfig>(builder.Configuration.GetSection(nameof(RabbitMQConfig)));
builder.Services.Configure<SlackConfig>(builder.Configuration.GetSection(nameof(SlackConfig)));
builder.Services.Configure<PayuConfig>(builder.Configuration.GetSection(nameof(PayuConfig)));
builder.Services.Configure<SendgridConfig>(builder.Configuration.GetSection(nameof(SendgridConfig)));


builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllers().AddNewtonsoftJson(o =>
{
    o.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    o.SerializerSettings.Converters.Add(new StringEnumConverter());
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromHours(4);
            options.SlidingExpiration = true;
            options.AccessDeniedPath = "/Forbidden/";
            options.LoginPath = "/Login";
            options.LogoutPath = "/Logout";
        })
    // this one is for API
    .AddScheme<HeaderTokenAuthSchemeOptions, HeaderTokenAuthHandler>(HeaderTokenAuthHandler.AuthSchemaName, options => { });

builder.Services.AddAuthorization(options => {

    });

builder.Services.AddHttpClient();
builder.Services.AddHttpClient(PayuService.HttpClientName, c =>
{
    c.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
    {
        NoCache = true,
        NoStore = true,
        MaxAge = new TimeSpan(0),
        MustRevalidate = true
    };
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false,
});

builder.Services.AddTransient<SendgridService>();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddScoped<UserLogic>();
builder.Services.AddScoped<SiteLogic>();
builder.Services.AddScoped<StatsLogic>();
builder.Services.AddScoped<OrganizationLogic>();
builder.Services.AddScoped<PaymentLogic>();
builder.Services.AddScoped<ReferralLogic>();
builder.Services.AddScoped<ISubscriptionPlanGuard, SubscriptionPlanGuard>();

builder.Services.AddTransient<IDbRepository, DbRepository>();
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

builder.Services.AddSingleton<IEsFactory, EsFactory>();
builder.Services.AddTransient<IEsRepository, EsRepository>();
builder.Services.AddTransient<IEsStatsRepository, EsStatsRepository>();

builder.Services.AddTransient<IMessagingService, MessagingService>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddTransient<PayuService>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddMvc()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

builder.Services.SetupApiDocs();

builder.Services.AddValidatorsFromAssemblyContaining<Program>(lifetime: ServiceLifetime.Scoped);
builder.Services.AddFluentValidationAutoValidation();

builder.Host.UseWindowsService();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

// TODO: global exception handler:
// https://dev.to/moe23/net-6-web-api-global-exceptions-handling-1a46
// https://jasonwatmore.com/post/2022/01/17/net-6-global-error-handler-tutorial-with-example

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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// TODO: api docs https://github.com/RicoSuter/NSwag/wiki/AspNetCore-Middleware
app.UseApiDocs();

await app.RunAsync();

static class StartupExtensions
{
    public static void SetupApiDocs(this IServiceCollection services)
    {
        services.AddSwaggerGen(c => {
            var groupNameV1 = "v1";
            c.SwaggerDoc(groupNameV1, new OpenApiInfo { Title = "StatsBro API", Version = "v1" });

            var currentAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var filePath = Path.Combine(System.AppContext.BaseDirectory, $"{currentAssemblyName}.xml");
            c.IncludeXmlComments(filePath, true);
            c.DocInclusionPredicate((string title, ApiDescription apiDesc) =>
            {
                // to include only controllers dedicated for API 
                return groupNameV1 == apiDesc.GroupName;
            });

            c.AddSecurityDefinition("api-key", new OpenApiSecurityScheme 
            {
                Type = SecuritySchemeType.ApiKey,
                Description = "Type into the textbox: {your api key}. The api key can be obtained from site settings in Sharing tab.",
                BearerFormat = "Bearer format here",
                In = ParameterLocation.Header,
                Name = "api-key",
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "api-key" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        services.AddFluentValidationRulesToSwagger();
    }

    public static void UseApiDocs(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.EnableValidator();
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "StatsBro API v1");
        });
    }
}