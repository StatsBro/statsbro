using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using StatsBro.Domain.Config;
using StatsBro.Panel.Logic;
using StatsBro.Panel.Services;
using StatsBro.Storage.Database;
using StatsBro.Storage.ElasticSearch;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(4);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = "/Forbidden/";
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
});

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

app.Run();
