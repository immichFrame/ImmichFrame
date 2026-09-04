using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using System.Reflection;
using ImmichFrame.Core.Logic;
using ImmichFrame.Core.Logic.AccountSelection;
using ImmichFrame.WebApi.Database;
using ImmichFrame.WebApi.Helpers;
using ImmichFrame.WebApi.Helpers.Config;
using ImmichFrame.WebApi.Models;
using ImmichFrame.WebApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
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
    // Only show HttpClient request info logs when LOG_LEVEL is Debug or lower
    if (level > LogLevel.Debug)
    {
        builder.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
    }
});


// Setup Config
var configPath = Environment.GetEnvironmentVariable("IMMICHFRAME_CONFIG_PATH") ??
        Directory.EnumerateDirectories(AppDomain.CurrentDomain.BaseDirectory, "*", SearchOption.TopDirectoryOnly)
        .FirstOrDefault(d => string.Equals(Path.GetFileName(d), "Config", StringComparison.OrdinalIgnoreCase))
        ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
builder.Services.AddTransient<ConfigLoader>();

// Settings live in a SQLite db in the config directory; file/env config is imported on first run
builder.Services.AddDbContextFactory<SettingsDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(configPath, "immichframe.db")}"));
builder.Services.AddSingleton(new SettingsServiceOptions(configPath));
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<ISettingsProvider>(srv => srv.GetRequiredService<SettingsService>());

// Register settings as live facades over the provider so config changes apply without restart
builder.Services.AddSingleton<IServerSettings, LiveServerSettings>();
builder.Services.AddSingleton<IGeneralSettings, LiveGeneralSettings>();
builder.Services.AddSingleton<IClientSettings>(srv => srv.GetRequiredService<IGeneralSettings>());
builder.Services.AddSingleton<IServerBehaviorSettings>(srv => srv.GetRequiredService<IGeneralSettings>());

// Register services
builder.Services.AddSingleton<IWeatherService, OpenWeatherMapService>();
builder.Services.AddSingleton<ICalendarService, IcalCalendarService>();
builder.Services.AddSingleton<IAssetAccountTracker, BloomFilterAssetAccountTracker>();
builder.Services.AddSingleton<Func<IList<IAccountImmichFrameLogic>, IAccountSelectionStrategy>>(srv =>
    accounts => ActivatorUtilities.CreateInstance<TotalAccountImagesSelectionStrategy>(srv, accounts));
builder.Services.AddHttpClient(); // Ensures IHttpClientFactory is available

builder.Services.AddTransient<Func<IAccountSettings, IAccountImmichFrameLogic>>(srv =>
    account => ActivatorUtilities.CreateInstance<PooledImmichFrameLogic>(srv, account));

// The account logic graph is frozen at construction; wrap it so it can be rebuilt on settings changes
builder.Services.AddSingleton<Func<IImmichFrameLogic>>(srv =>
    () => ActivatorUtilities.CreateInstance<MultiImmichFrameLogicDelegate>(srv));
builder.Services.AddSingleton<IImmichFrameLogic, ReloadingImmichFrameLogic>();

builder.Services.AddControllers()
      .AddJsonOptions(options =>
          options.JsonSerializerOptions.Converters.Add(
              new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SchemaFilter<ImmichFrame.WebApi.Helpers.NoReadOnlySchemaFilter>());

builder.Services.AddAuthorization(options => { options.AddPolicy("AllowAnonymous", policy => policy.RequireAssertion(context => true)); });

builder.Services.AddSingleton<AdminAuthService>();

builder.Services.AddAuthentication("ImmichFrameScheme")
    .AddScheme<AuthenticationSchemeOptions, ImmichFrameAuthenticationHandler>("ImmichFrameScheme", options => { })
    .AddScheme<AuthenticationSchemeOptions, ImmichFrameAdminAuthenticationHandler>(ImmichFrameAdminAuthenticationHandler.SchemeName, options => { });

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

if (app.Environment.IsDevelopment())
{
    var root = Directory.GetCurrentDirectory();
    var dotenv = Path.Combine(root, "..", "docker", ".env");

    dotenv = Path.GetFullPath(dotenv);
    DotEnv.Load(dotenv);
}

// app.UseHttpsRedirection();
app.UseMiddleware<CustomAuthenticationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

// Skipped when tests replace ISettingsProvider with a stub
if (app.Services.GetRequiredService<ISettingsProvider>() is SettingsService settingsService)
{
    await settingsService.InitializeAsync();
}

// Deliberately not awaited: an unreachable Immich server must not delay startup, otherwise
// the admin UI needed to fix that very server stays unreachable too.
_ = Task.Run(async () =>
{
    try
    {
        var immichServersOk = await ImmichServerVersionChecker.CheckServerVersions(app.Services, app.Logger);
        if (!immichServersOk)
        {
            app.Logger.LogCritical("One or more Immich servers are unreachable or unsupported (see log above). The slideshow may not work — fix the account settings via the admin UI at /admin.");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical("Immich server version check failed: {Message}", ex.Message);
    }
});

app.Run();

// Make Program public for WebApplicationFactory
public partial class Program { }
