using System.Runtime.CompilerServices;

namespace ImmichFrame.WebApi.Services;

/// <summary>Ensures the entry assembly's hosting-startup attribute is discovered.</summary>
internal static class TranscoderHostingStartupBootstrap
{
    [ModuleInitializer]
    internal static void Enable()
    {
        const string variable = "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES";
        var assembly = typeof(TranscoderStartup).Assembly.GetName().Name!;
        var current = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(current))
        {
            Environment.SetEnvironmentVariable(variable, assembly);
            return;
        }

        if (!current.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(assembly, StringComparer.OrdinalIgnoreCase))
            Environment.SetEnvironmentVariable(variable, current + ";" + assembly);
    }
}
