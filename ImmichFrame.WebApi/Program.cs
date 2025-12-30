using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Authentication;
using System.Reflection;
using ImmichFrame.Core.Logic;
using ImmichFrame.Core.Logic.AccountSelection;
using ImmichFrame.WebApi.Data;
using ImmichFrame.WebApi.Helpers.Config;
using ImmichFrame.WebApi.Services;
using Microsoft.EntityFrameworkCore;

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

// SQLite-backed override store (single row)
builder.Services.AddDbContext<ConfigDbContext>(options =>
{
    var configuredPath = Environment.GetEnvironmentVariable("IMMICHFRAME_DB_PATH");
    var defaultPath = Directory.Exists("/data")
        ? Path.Combine("/data", "immichframe.db")
        : Path.Combine(AppContext.BaseDirectory, "data", "immichframe.db");

    var dbPath = string.IsNullOrWhiteSpace(configuredPath) ? defaultPath : configuredPath;

    var dbDir = Path.GetDirectoryName(dbPath);
    if (!string.IsNullOrWhiteSpace(dbDir))
    {
        Directory.CreateDirectory(dbDir);
    }

    options.UseSqlite($"Data Source={dbPath}");
});
builder.Services.AddScoped<IAccountOverrideStore, SqliteAccountOverrideStore>();

// Register services
builder.Services.AddSingleton<IWeatherService, OpenWeatherMapService>();
builder.Services.AddSingleton<ICalendarService, IcalCalendarService>();
builder.Services.AddSingleton<IAssetAccountTracker, BloomFilterAssetAccountTracker>();
builder.Services.AddTransient<IAccountSelectionStrategy, TotalAccountImagesSelectionStrategy>();
builder.Services.AddHttpClient(); // Ensures IHttpClientFactory is available

builder.Services.AddTransient<Func<IAccountSettings, IAccountImmichFrameLogic>>(srv =>
    account => ActivatorUtilities.CreateInstance<PooledImmichFrameLogic>(srv, account));

builder.Services.AddSingleton<IImmichFrameLogic, ReloadingImmichFrameLogic>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization(options => { options.AddPolicy("AllowAnonymous", policy => policy.RequireAssertion(context => true)); });

builder.Services.AddAuthentication("ImmichFrameScheme")
    .AddScheme<AuthenticationSchemeOptions, ImmichFrameAuthenticationHandler>("ImmichFrameScheme", options => { });

var app = builder.Build();

// Ensure DB exists (prefer migrations when present; otherwise create schema)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        // Check if we have any migrations to apply
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count);
            db.Database.Migrate();
        }
        else
        {
            // No pending migrations - check if table exists, if not create it manually
            logger.LogInformation("No pending migrations, checking if AccountOverrides table exists");
            var tableExists = db.Database.ExecuteSqlRaw(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='AccountOverrides'") > 0;
            
            if (!tableExists)
            {
                logger.LogInformation("AccountOverrides table does not exist, creating it manually");
                // Create the table manually since EnsureCreated() doesn't work when migrations history exists
                db.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS AccountOverrides (
                        Id INTEGER NOT NULL PRIMARY KEY,
                        ShowMemories INTEGER,
                        ShowFavorites INTEGER,
                        ShowArchived INTEGER,
                        ImagesFromDays INTEGER,
                        ImagesFromDate TEXT,
                        ImagesUntilDate TEXT,
                        Albums TEXT,
                        ExcludedAlbums TEXT,
                        People TEXT,
                        Rating INTEGER,
                        UpdatedAtUtc TEXT NOT NULL
                    )");
                logger.LogInformation("AccountOverrides table created successfully");
            }
            else
            {
                logger.LogInformation("AccountOverrides table already exists");
            }
        }
    }
    catch (Exception ex)
    {
        // Fallback: ensure created if migrations fail
        logger.LogError(ex, "Database initialization failed, attempting EnsureCreated");
        try
        {
            db.Database.EnsureCreated();
            logger.LogInformation("Fallback EnsureCreated() completed");
        }
        catch (Exception ex2)
        {
            logger.LogError(ex2, "EnsureCreated also failed");
            throw;
        }
    }
}

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

app.Run();

// Make Program public for WebApplicationFactory
public partial class Program { }
