using System.Net;
using ImmichFrame.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Controllers;

public class FrameControlsControllerTests
{
    private sealed class FrameHandler : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        public bool RespondAsFrame { get; set; } = true;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Paths.Add(request.RequestUri!.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RespondAsFrame ? "Brightness parameter missing or invalid" : "Unknown service")
            });
        }
    }

    [TestCase("192.168.1.8", "brightness", 0.375, 204, "/brightness?value=0.375")]
    [TestCase("192.168.1.8", "brightness", 0, 204, "/brightness?value=0")]
    [TestCase("192.168.1.8", "brightness", 1, 204, "/brightness?value=1")]
    [TestCase("192.168.1.8", "brightness", -0.1, 400, null)]
    [TestCase("192.168.1.8", "brightness", 1.01, 400, null)]
    [TestCase("192.168.1.8", "brightness", -1, 204, "/brightness?value=-1")]
    [TestCase("192.168.1.8", "dim", 0, 204, "/dim")]
    [TestCase("192.168.1.8", "undim", 0, 204, "/undim")]
    [TestCase("192.168.2.8", "next", 0, 400, null)]
    [TestCase("192.168.1.255", "next", 0, 400, null)]
    [TestCase("192.168.1.8", "brightness", 2, 400, null)]
    [TestCase("192.168.1.8", "other", 0, 400, null)]
    [TestCase("192.168.1.8", "next", 0, 502, null)]
    public async Task SendValidatesTargetAndCommand(string device, string command, decimal value, int status, string? expectedPath)
    {
        using var handler = new FrameHandler { RespondAsFrame = status != 502 };
        using var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("FrameRemote")).Returns(client);
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["FRAME_CONTROL_NETWORK"] = "192.168.1.0/24" }).Build();
        var controller = new FrameControlsController(factory.Object, config);
        var result = await controller.Send(new(device, command, value), CancellationToken.None);
        var actual = ((Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)result).StatusCode;
        Assert.That(actual, Is.EqualTo(status));
        string[] expected = expectedPath != null ? ["/brightness", expectedPath] : status == 502 ? ["/brightness"] : [];
        Assert.That(handler.Paths, Is.EqualTo(expected));
    }
}
