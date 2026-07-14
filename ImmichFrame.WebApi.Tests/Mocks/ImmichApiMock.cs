using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ImmichFrame.WebApi.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Moq;
using Moq.Protected;

namespace ImmichFrame.WebApi.Tests.Mocks
{
    /// <summary>
    /// Shared test helpers for mocking the Immich API behind ImmichFrame's <c>ImmichApiAccountClient</c>.
    /// </summary>
    public static class ImmichApiMock
    {
        /// <summary>
        /// Sets up the handler to answer <c>GET server/version</c> with a compatible Immich server version,
        /// so the startup version gate allows ImmichFrame to start during tests.
        /// </summary>
        public static Mock<HttpMessageHandler> WithServerVersion(
            this Mock<HttpMessageHandler> handler,
            long major = ImmichServerVersionChecker.MinimumSupportedMajorVersion,
            long minor = 0,
            long patch = 0)
        {
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("server/version")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $"{{\"major\":{major},\"minor\":{minor},\"patch\":{patch},\"prerelease\":null}}")
                });

            return handler;
        }

        /// <summary>
        /// Registers the mock handler as the primary <see cref="HttpMessageHandler"/> for every named
        /// <see cref="System.Net.Http.HttpClient"/> created by <see cref="IHttpClientFactory"/>.
        /// </summary>
        public static IServiceCollection UseMockHandler(this IServiceCollection services, Mock<HttpMessageHandler> handler)
        {
            services.ConfigureAll<HttpClientFactoryOptions>(options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(b =>
                {
                    b.PrimaryHandler = handler.Object;
                });
            });

            return services;
        }
    }
}
