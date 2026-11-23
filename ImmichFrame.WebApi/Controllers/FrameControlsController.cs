using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers;

[ApiController]
[Route("api/controls")]
public sealed class FrameControlsController(IHttpClientFactory clients, IConfiguration configuration) : ControllerBase
{
    private static readonly SemaphoreSlim ScanGate = new(1);

    // A bridge-mode container can route to the host LAN, but cannot infer its prefix
    // from its own interfaces. Prefer the operator's LAN, otherwise use the client LAN.
    private IPNetwork? GetNetwork()
    {
        var configured = configuration["FRAME_CONTROL_NETWORK"];
        IPNetwork network;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (!IPNetwork.TryParse(configured, out network) || network.PrefixLength != 24)
                return null;
        }
        else
        {
            var peer = HttpContext.Connection.RemoteIpAddress;
            if (peer?.IsIPv4MappedToIPv6 == true) peer = peer.MapToIPv4();
            if (peer?.AddressFamily != AddressFamily.InterNetwork) return null;
            var bytes = peer.GetAddressBytes();
            bytes[3] = 0;
            network = new IPNetwork(new IPAddress(bytes), 24);
        }
        var prefix = network.BaseAddress.GetAddressBytes();
        return prefix.Length == 4 && (prefix[0] == 10 || prefix[0] == 192 && prefix[1] == 168
            || prefix[0] == 172 && prefix[1] is >= 16 and <= 31) ? network : null;
    }

    private static async Task<bool> IsFrame(HttpClient client, IPAddress address, CancellationToken ct)
    {
        try
        {
            // Without a value Android returns a diagnostic without changing brightness.
            using var response = await client.GetAsync($"http://{address}:53287/brightness", ct);
            return response.IsSuccessStatusCode && (await response.Content.ReadAsStringAsync(ct))
                .StartsWith("Brightness parameter missing or invalid", StringComparison.Ordinal);
        }
        catch (HttpRequestException) { return false; }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return false; }
    }

    [AllowAnonymous]
    [HttpPost("scan")]
    public async Task<IActionResult> Scan(CancellationToken ct)
    {
        if (GetNetwork() is not { } network)
            return BadRequest("Set FRAME_CONTROL_NETWORK to the frame LAN, for example 192.168.1.0/24.");
        if (!await ScanGate.WaitAsync(0, ct)) return StatusCode(429, "A scan is already running.");
        try
        {
            using var client = clients.CreateClient("FrameRemote");
            var hosts = new IPAddress?[255];
            await Parallel.ForEachAsync(Enumerable.Range(1, 254),
                new ParallelOptions { MaxDegreeOfParallelism = 24, CancellationToken = ct },
                async (host, token) => {
                    var bytes = network.BaseAddress.GetAddressBytes();
                    bytes[3] = (byte)host;
                    var address = new IPAddress(bytes);
                    if (await IsFrame(client, address, token)) hosts[host] = address;
                });
            return Ok(hosts.OfType<IPAddress>().Select(address => address.ToString()).ToArray());
        }
        finally { ScanGate.Release(); }
    }

    public record FrameCommand(string Device, string Command, decimal? Value);

    [Authorize]
    [HttpPost("command")]
    public async Task<IActionResult> Send(FrameCommand request, CancellationToken ct)
    {
        if (!IPAddress.TryParse(request.Device, out var address) || address.AddressFamily != AddressFamily.InterNetwork
            || GetNetwork() is not { } network || !network.Contains(address) || address.GetAddressBytes()[3] is 0 or 255)
            return BadRequest("Select a frame on the configured LAN.");
        var path = request.Command switch
        {
            "dim" or "undim" or "next" or "previous" or "pause" or "settings" => request.Command,
            "brightness" when request.Value is -1 or (>= 0 and <= 1) =>
                "brightness?value=" + request.Value.Value.ToString(CultureInfo.InvariantCulture),
            _ => null
        };
        if (path == null) return BadRequest("Invalid frame command.");
        using var client = clients.CreateClient("FrameRemote");
        if (!await IsFrame(client, address, ct)) return StatusCode(502, "Frame unavailable. Scan again.");
        try
        {
            using var response = await client.GetAsync($"http://{address}:53287/{path}", ct);
            return response.IsSuccessStatusCode ? NoContent() : StatusCode(502, "Frame rejected the command.");
        }
        catch (HttpRequestException) { return StatusCode(502, "Frame connection failed."); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return StatusCode(504, "Frame timed out."); }
    }
}
