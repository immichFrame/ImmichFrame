using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(ImmichFrame.WebApi.Services.TranscoderTimeoutStartup))]

namespace ImmichFrame.WebApi.Services;

/// <summary>Bounds the optional sidecar lookup so playback always degrades to Immich promptly.</summary>
public sealed class TranscoderTimeoutStartup : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
            services.AddHttpClient("Transcoder", client => client.Timeout = TimeSpan.FromSeconds(2)));
    }
}
