using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Authentication;
using System.Reflection;
using ImmichFrame.Core.Logic;
using ImmichFrame.Core.Logic.AccountSelection;
using ImmichFrame.WebApi.Helpers;
using ImmichFrame.WebApi.Helpers.Config;
using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    var root = Directory.GetCurrentDirectory();
    var dotenv = Path.Combine(root, "..", "docker", ".env");

    dotenv = Path.GetFullPath(dotenv);
    DotEnv.Load(dotenv);
}

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://+:8080");
}

//log the version number
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
Console.WriteLine($@"
 _                     _      _    ______                        
(_)                   (_)    | |   |  ___|                       
 _ _ __ ___  _ __ ___  _  ___| |__ | |_ _ __ __ _ _ __ ___   ___ 
| | '_ ` _ \| '_ ` _ \| |/ __| '_ \|  _| '__/ _` | '_ ` _ \ / _ \
| | | | | | | | | | | | | (__| | | | | | | | (_| | | | | | |  __/
|_|_| |_| |_|_| |_| |_|_|\___|_| |_\_| |_|  \__,_|_| |_| |_|\___| Version {version}");
Console.WriteLine();

// Add services to the container.
builder.Services.AddLogging(builder =>
{
    LogLevel level = LogLevel.Information;
    var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL");
    if (!string.IsNullOrWhiteSpace(logLevel))
    {
        Enum.TryParse(logLevel, true, out level);
    }

    Console.WriteLine($"LogLevel: {level}");
    builder.SetMinimumLevel(level);
    builder.AddSimpleConsole(options =>
    {
        // Customizing the log output format
        options.TimestampFormat = "yy-MM-dd HH:mm:ss "; // Custom timestamp format
        options.SingleLine = true;
    });

    // Disable SpaProxy info logs
    builder.AddFilter("Microsoft.AspNetCore.SpaProxy", LogLevel.Warning);
    // Disable AspNetCore info logs
    builder.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
});


// Setup Config
var configPath = Environment.GetEnvironmentVariable("IMMICHFRAME_CONFIG_PATH") ??
        Directory.EnumerateDirectories(AppDomain.CurrentDomain.BaseDirectory, "*", SearchOption.TopDirectoryOnly)
        .FirstOrDefault(d => string.Equals(Path.GetFileName(d), "Config", StringComparison.OrdinalIgnoreCase))
        ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
builder.Services.AddTransient<ConfigLoader>();
builder.Services.AddSingleton<IServerSettings>(srv => srv.GetRequiredService<ConfigLoader>().LoadConfig(configPath));

// Register sub-settings
builder.Services.AddSingleton<IGeneralSettings>(srv => srv.GetRequiredService<IServerSettings>().GeneralSettings);

// Register services
builder.Services.AddSingleton<IWeatherService, OpenWeatherMapService>();
builder.Services.AddSingleton<ICalendarService, IcalCalendarService>();
builder.Services.AddSingleton<IAssetAccountTracker, BloomFilterAssetAccountTracker>();
builder.Services.AddSingleton<IAccountSelectionStrategy, TotalAccountImagesSelectionStrategy>();
builder.Services.AddHttpClient(); // Ensures IHttpClientFactory is available

builder.Services.AddTransient<Func<IAccountSettings, IAccountImmichFrameLogic>>(srv =>
    account => ActivatorUtilities.CreateInstance<PooledImmichFrameLogic>(srv, account));

builder.Services.AddSingleton<IImmichFrameLogic, MultiImmichFrameLogicDelegate>();
var appDataPath = Environment.GetEnvironmentVariable("IMMICHFRAME_APP_DATA_PATH") ??
    Path.Combine(builder.Environment.ContentRootPath, "App_Data");
builder.Services.AddSingleton(new FrameSessionRegistryOptions
{
    DisplayNameStorePath = Path.Combine(appDataPath, "frame-session-display-names.json")
});
builder.Services.AddSingleton<IFrameSessionRegistry>(srv =>
    new FrameSessionRegistry(
        srv.GetRequiredService<FrameSessionRegistryOptions>(),
        null,
        srv.GetService<ILogger<FrameSessionRegistry>>()));
builder.Services.AddSingleton<IAdminBasicAuthService, AdminBasicAuthService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AdminLogin", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));
});

builder.Services.AddAuthorization();

builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, ImmichFrameAuthenticationHandler>(AuthSchemes.Frame, options => { })
    .AddCookie(AuthSchemes.Admin, options =>
    {
        options.Cookie.Name = "ImmichFrame.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = context =>
            {
                var adminBasicAuthService = context.HttpContext.RequestServices.GetRequiredService<IAdminBasicAuthService>();
                var username = context.Principal?.FindFirstValue(ClaimTypes.Name);
                var credentialVersion = context.Principal?.FindFirst("immichframe_admin_credential_version")?.Value;
                var currentVersion = string.IsNullOrWhiteSpace(username)
                    ? null
                    : adminBasicAuthService.GetCredentialVersion(username);

                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(credentialVersion) ||
                    string.IsNullOrWhiteSpace(currentVersion) ||
                    !string.Equals(credentialVersion, currentVersion, StringComparison.Ordinal))
                {
                    context.RejectPrincipal();
                    return context.HttpContext.SignOutAsync(AuthSchemes.Admin);
                }

                return Task.CompletedTask;
            },
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
if (app.Environment.IsProduction())
{
    app.UseDefaultFiles();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

// Make Program public for WebApplicationFactory
public partial class Program { }
