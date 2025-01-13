using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
using ImmichFrame.WebApi.Models;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

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

builder.Services.AddSingleton<IServerSettings>(srv =>
{
    if (serverSettings == null)
        serverSettings = new ServerSettings();

    return serverSettings;
});

builder.Services.AddSingleton(srv =>
{
    var settings = srv.GetRequiredService<IServerSettings>();

    return new ImmichFrameLogic(settings);
});

builder.Services.AddSingleton<IWebClientSettings>(srv =>
{
    if (clientSettings == null)
        clientSettings = new WebClientSettings();

    return clientSettings;
});

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


