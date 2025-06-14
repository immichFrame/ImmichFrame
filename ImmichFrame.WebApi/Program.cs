using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Authentication;
using System.Reflection;
using ImmichFrame.Core.Logic;
using ImmichFrame.Core.Logic.AccountSelection;
using ImmichFrame.WebApi.Helpers;
using ImmichFrame.WebApi.Helpers.Config;

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
var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Settings.json");
builder.Services.AddTransient<ConfigLoader>();
builder.Services.AddSingleton<IServerSettings>(srv => srv.GetRequiredService<ConfigLoader>().LoadConfig(settingsPath));

// Register sub-settings
builder.Services.AddSingleton<IGeneralSettings>(srv => srv.GetRequiredService<IServerSettings>().GeneralSettings);

// Register services
builder.Services.AddSingleton<IWeatherService, OpenWeatherMapService>();
builder.Services.AddSingleton<ICalendarService, IcalCalendarService>();
builder.Services.AddSingleton<IAssetAccountTracker, BloomFilterAssetAccountTracker>();
builder.Services.AddSingleton<IAccountSelectionStrategy, TotalAccountImagesSelectionStrategy>();
builder.Services.AddTransient<Func<IAccountSettings, IImmichFrameLogic>>(srv => account => ActivatorUtilities.CreateInstance<PooledImmichFrameLogic>(srv, account));
builder.Services.AddSingleton<IImmichFrameLogic, MultiImmichFrameLogicDelegate>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization(options => { options.AddPolicy("AllowAnonymous", policy => policy.RequireAssertion(context => true)); });

builder.Services.AddAuthentication("ImmichFrameScheme")
    .AddScheme<AuthenticationSchemeOptions, ImmichFrameAuthenticationHandler>("ImmichFrameScheme", options => { });

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

app.Run();