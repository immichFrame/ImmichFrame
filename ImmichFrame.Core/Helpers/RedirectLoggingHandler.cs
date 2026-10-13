using System.Net;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.Core.Helpers;

/// <summary>
/// Logs redirects that the Immich API client refuses to follow, so a misconfigured
/// <c>ImmichServerUrl</c> reads as a redirect rather than as an unexplained failure.
/// </summary>
public class RedirectLoggingHandler(ILogger<RedirectLoggingHandler> logger) : DelegatingHandler
{
    /// <summary>
    /// Set on a request whose redirect is part of the Immich API contract (e.g. <c>size=fullsize</c>
    /// answering with a redirect to the preview), so it is logged at Debug instead of as a misconfiguration.
    /// </summary>
    public static readonly HttpRequestOptionsKey<bool> RedirectExpected = new("ImmichFrame.RedirectExpected");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (IsRedirect(response.StatusCode) &&
            request.Options.TryGetValue(RedirectExpected, out var expected) && expected)
        {
            logger.LogDebug("Immich server answered {Method} {Url} with an expected {StatusCode} to {Location}.",
                request.Method, request.RequestUri, (int)response.StatusCode, response.Headers.Location);
        }
        else if (IsRedirect(response.StatusCode))
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
