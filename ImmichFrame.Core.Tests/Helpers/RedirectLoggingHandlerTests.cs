using System.Net;
using ImmichFrame.Core.Helpers;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Helpers;

/// <summary>
/// A redirect the Immich API answers by design (size=fullsize without a full-size file) must not
/// read as the "misconfigured ImmichServerUrl" warning; any other redirect still must.
/// </summary>
[TestFixture]
public class RedirectLoggingHandlerTests
{
    [TestCase(true, LogLevel.Debug)]
    [TestCase(false, LogLevel.Warning)]
    public async Task Redirect_IsLoggedAtDebugOnlyWhenExpected(bool expected, LogLevel level)
    {
        var logger = new ListLogger();
        using var invoker = new HttpMessageInvoker(new RedirectLoggingHandler(logger)
        {
            InnerHandler = new FoundHandler()
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "http://immich.test/api/assets/1/thumbnail?size=fullsize");
        if (expected)
            request.Options.Set(RedirectLoggingHandler.RedirectExpected, true);

        using var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.That(logger.Levels, Is.EqualTo(new[] { level }));
    }

    private sealed class FoundHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Found);
            response.Headers.Location = new Uri("thumbnail?size=preview", UriKind.Relative);
            return Task.FromResult(response);
        }
    }

    private sealed class ListLogger : ILogger<RedirectLoggingHandler>
    {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Levels.Add(logLevel);
    }
}
