using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
namespace ImmichFrame.Core.Tests.Logic.Pool;

public static class FixtureHelpers
{
    // v3 changed asset/album/person ids from string to Guid. Tests still want
    // readable, stable ids (e.g. "p1_0") for arranging mocks and asserting
    // membership, so we deterministically derive a Guid from a seed string:
    // the same seed always maps to the same Guid.
    public static Guid GuidFor(string seed) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes(seed)));

    public static ILogger<T> TestLogger<T>()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        return loggerFactory.CreateLogger<T>();
    }
}