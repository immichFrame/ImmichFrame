using System.Net;
using System.Net.Sockets;
using System.Text;
using ImmichFrame.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Helpers;

/// <summary>
/// The Immich API key travels in the non-standard X-API-KEY header, which the runtime does not
/// recognise as a credential and would replay across a redirect to any other origin. These tests
/// pin the registration that prevents that.
/// </summary>
[TestFixture]
public class ImmichApiHttpClientExtensionsTests
{
    private const string ApiKeyHeader = "X-API-KEY";
    private const string ApiKeySentinel = "immich-key-sentinel";

    private static HttpClient CreateRegisteredClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddImmichApiHttpClient();

        var factory = services.BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();

        var client = factory.CreateClient(ImmichApiHttpClientExtensions.ImmichApiAccountClient);
        client.UseApiKey(ApiKeySentinel);
        return client;
    }

    [Test]
    public async Task NamedClient_DoesNotFollowRedirect_AndDoesNotLeakApiKey()
    {
        using var origin = new StubServer(request =>
            $"HTTP/1.1 302 Found\r\nLocation: {LoopbackUrl(RedirectTargetPort)}/leak\r\nContent-Length: 0\r\n\r\n");
        using var redirectTarget = new StubServer(_ => "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n");

        origin.Start();
        redirectTarget.Start(RedirectTargetPort);

        using var client = CreateRegisteredClient();
        var response = await client.GetAsync($"{LoopbackUrl(origin.Port)}/api/search/metadata");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Found),
                "The redirect must be handed back to the caller, not followed.");
            Assert.That(redirectTarget.ReceivedRequests, Is.Empty,
                "The redirect target must never be contacted.");
            Assert.That(origin.ReceivedRequests.Single(), Does.Contain(ApiKeySentinel),
                "Sanity check: the API key is sent to the configured origin.");
        });
    }

    [Test]
    public async Task NamedClient_SendsApiKeyToConfiguredOrigin()
    {
        using var origin = new StubServer(_ => "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n");
        origin.Start();

        using var client = CreateRegisteredClient();
        var response = await client.GetAsync($"{LoopbackUrl(origin.Port)}/api/server/version");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(origin.ReceivedRequests.Single(), Does.Contain($"{ApiKeyHeader}: {ApiKeySentinel}"));
    }

    // A fixed port is needed because the redirect Location has to be written before the target listens.
    private const int RedirectTargetPort = 18099;

    private static string LoopbackUrl(int port) => $"http://127.0.0.1:{port}";

    /// <summary>Minimal HTTP server that records raw requests and replies with a canned response.</summary>
    private sealed class StubServer(Func<string, string> respond) : IDisposable
    {
        private readonly List<string> _received = [];
        private readonly CancellationTokenSource _cts = new();
        private TcpListener? _listener;

        public int Port { get; private set; }

        public IReadOnlyList<string> ReceivedRequests
        {
            get { lock (_received) return _received.ToList(); }
        }

        public void Start(int port = 0)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = AcceptLoopAsync(_listener, _cts.Token);
        }

        private async Task AcceptLoopAsync(TcpListener listener, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient connection;
                try { connection = await listener.AcceptTcpClientAsync(ct); }
                catch (OperationCanceledException) { return; }
                catch (ObjectDisposedException) { return; }
                catch (SocketException) { return; }

                using (connection)
                await using (var stream = connection.GetStream())
                {
                    var buffer = new byte[8192];
                    var read = await stream.ReadAsync(buffer, ct);
                    var request = Encoding.ASCII.GetString(buffer, 0, read);
                    lock (_received) _received.Add(request);

                    var reply = Encoding.ASCII.GetBytes(respond(request));
                    await stream.WriteAsync(reply, ct);
                    await stream.FlushAsync(ct);
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener?.Stop();
            _cts.Dispose();
        }
    }
}
