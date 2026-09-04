using System.Net;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Helpers;

/// <summary>
/// Logs redirects that the Immich API client refuses to follow, so a misconfigured
/// <c>ImmichServerUrl</c> reads as a redirect rather than as an unexplained failure.
/// </summary>
public class RedirectLoggingHandler(ILogger<RedirectLoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (IsRedirect(response.StatusCode))
        {
            logger.LogWarning(
                "Immich server answered {Method} {Url} with {StatusCode}, redirecting to {Location}. " +
                "The redirect was not followed, because the API key must not be sent to another host. " +
                "Point ImmichServerUrl at the final URL instead.",
                request.Method, request.RequestUri, (int)response.StatusCode, response.Headers.Location);
        }

        return response;
    }

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther or
        HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
}
