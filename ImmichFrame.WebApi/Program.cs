using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json;
using System.Reflection;

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

var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Settings.json");

ServerSettings? serverSettings = null;
WebClientSettings? clientSettings = null;

if (File.Exists(settingsPath))
{
    var json = File.ReadAllText(settingsPath);
    JsonDocument doc;
    try
    {
        doc = JsonDocument.Parse(json);
    }
    catch (Exception ex)
    {
        throw new SettingsNotValidException($"Problem with parsing the settings: {ex.Message}", ex);
    }

    serverSettings = JsonSerializer.Deserialize<ServerSettings>(doc);
    clientSettings = JsonSerializer.Deserialize<WebClientSettings>(doc);
}

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
                options.TimestampFormat = "yy-MM-dd HH:mm:ss ";  // Custom timestamp format
                options.SingleLine = true;
            });

    // Disable SpaProxy info logs
    builder.AddFilter("Microsoft.AspNetCore.SpaProxy", LogLevel.Warning);
    // Disable AspNetCore info logs
    builder.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
});

builder.Services.AddSingleton<IServerSettings, ServerSettings>();
builder.Services.AddSingleton<IWeatherService, OpenWeatherMapService>();
builder.Services.AddSingleton<ICalendarService, IcalCalendarService>();
builder.Services.AddSingleton<IImmichFrameLogic, OptimizedImmichFrameLogic>();
builder.Services.AddSingleton<IWebClientSettings, WebClientSettings>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AllowAnonymous", policy => policy.RequireAssertion(context => true));
    });

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


