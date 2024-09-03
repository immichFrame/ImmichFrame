using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.json");

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

builder.Services.AddSingleton<IBaseSettings>(srv =>
{
    var settings = JsonSerializer.Deserialize<Settings>(doc);

    if (settings == null)
        throw new FileNotFoundException();

    return settings;
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(x => x
.AllowAnyMethod()
.AllowAnyHeader()
.SetIsOriginAllowed(origin => true) // allow any origin
//.WithOrigins("https://localhost:44351")); // Allow only this origin can also have multiple origins separated with comma
//.AllowCredentials()// allow credentials
);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
