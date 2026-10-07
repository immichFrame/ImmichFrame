using Microsoft.Extensions.DependencyInjection;

namespace ImmichFrame.Core.Helpers;

public static class ImmichApiHttpClientExtensions
{
    public const string ImmichApiAccountClient = "ImmichApiAccountClient";

    public static IHttpClientBuilder AddImmichApiHttpClient(this IServiceCollection services)
    {
        services.AddTransient<RedirectLoggingHandler>();

        return services.AddHttpClient(ImmichApiAccountClient)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .AddHttpMessageHandler<RedirectLoggingHandler>();
    }
}
