using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ImmichFrame.Core.Api;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.Core.Logic;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.Core.Tests.Logic;

/// <summary>
/// The ImageSize setting decides which Immich endpoint an image is fetched from. These tests run the
/// real logic and ImmichApi against a fake HTTP handler and pin the requests it makes.
/// </summary>
[TestFixture]
public class PooledImmichFrameLogicImageSizeTests
{
    private static readonly byte[] Body = Encoding.ASCII.GetBytes("image-bytes");

    private Guid _id;
    private RecordingHandler _handler = null!;
    private Mock<IGeneralSettings> _general = null!;

    [SetUp]
    public void Setup()
    {
        _id = Guid.NewGuid();
        _general = new Mock<IGeneralSettings>();
        _general.SetupGet(s => s.RefreshAlbumPeopleInterval).Returns(12);
        _general.SetupGet(s => s.DownloadImages).Returns(false);
    }

    private PooledImmichFrameLogic CreateLogic(string? imageSize, Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _general.SetupGet(s => s.ImageSize).Returns(imageSize!);
        _handler = new RecordingHandler(respond);

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_handler));

        var account = new Mock<IAccountSettings>();
        account.SetupGet(a => a.ImmichServerUrl).Returns("http://immich.test");
        account.SetupGet(a => a.ApiKey).Returns("key");

        return new PooledImmichFrameLogic(account.Object, _general.Object, factory.Object);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("preview")]
    [TestCase("PREVIEW")]
    [TestCase("bogus")]
    public async Task Preview_IsTheDefault_AndUnknownValuesFallBackToIt(string? imageSize)
    {
        var logic = CreateLogic(imageSize, _ => ImageFile("image/webp"));

        var result = await logic.GetAsset(_id, AssetTypeEnum.IMAGE);

        Assert.Multiple(() =>
        {
            Assert.That(_handler.Requests, Has.Count.EqualTo(1));
            Assert.That(IsThumbnail(_handler.Requests[0], "preview"), Is.True, _handler.Requests[0].ToString());
            Assert.That(result.ContentType, Is.EqualTo("image/webp"));
            Assert.That(result.FileName, Is.EqualTo($"{_id}.webp"), "The preview keeps the pre-ImageSize file name.");
        });
    }

    [Test]
    public async Task Original_DownloadsTheOriginal_WhateverItsFormat()
    {
        var logic = CreateLogic("original", _ => ImageFile("image/heic"));

        var result = await logic.GetAsset(_id, AssetTypeEnum.IMAGE);

        Assert.Multiple(() =>
        {
            Assert.That(_handler.Requests.Select(r => r.AbsolutePath), Is.EqualTo(new[] { $"/api/assets/{_id}/original" }));
            Assert.That(result.ContentType, Is.EqualTo("image/heic"));
            Assert.That(result.FileName, Is.EqualTo($"{_id}_original.heic"));
            Assert.That(ReadAll(result.FileStream), Is.EqualTo(Body));
        });
    }

    [Test]
    public async Task Fullsize_WebSupportedOriginal_DownloadsTheOriginal()
    {
        var logic = CreateLogic("fullsize", request => request.RequestUri!.AbsolutePath == $"/api/assets/{_id}"
            ? AssetInfo("image/jpeg")
            : ImageFile("image/jpeg"));

        var result = await logic.GetAsset(_id, AssetTypeEnum.IMAGE);

        Assert.Multiple(() =>
        {
            Assert.That(_handler.Requests.Select(r => r.AbsolutePath),
                Is.EqualTo(new[] { $"/api/assets/{_id}", $"/api/assets/{_id}/original" }));
            Assert.That(result.ContentType, Is.EqualTo("image/jpeg"));
            Assert.That(result.FileName, Is.EqualTo($"{_id}_fullsize.jpeg"));
        });
    }

    [Test]
    public async Task Fullsize_HeicWithoutFullsizeRendition_FallsBackToPreview()
    {
        var logic = CreateLogic("fullsize", request =>
        {
            if (request.RequestUri!.AbsolutePath == $"/api/assets/{_id}") return AssetInfo("image/heic");
            if (IsThumbnail(request.RequestUri, "fullsize")) return Redirect("thumbnail?size=preview");
            return ImageFile("image/webp");
        });

        var result = await logic.GetAsset(_id, AssetTypeEnum.IMAGE);

        Assert.Multiple(() =>
        {
            Assert.That(_handler.Requests, Has.Count.EqualTo(3));
            Assert.That(IsThumbnail(_handler.Requests[1], "fullsize"), Is.True, _handler.Requests[1].ToString());
            Assert.That(IsThumbnail(_handler.Requests[2], "preview"), Is.True, _handler.Requests[2].ToString());
            Assert.That(_handler.Requests.Any(r => r.AbsolutePath.EndsWith("/original")), Is.False,
                "A HEIC original must not be sent in fullsize mode.");
            Assert.That(result.ContentType, Is.EqualTo("image/webp"));
        });
    }

    [Test]
    public async Task Fullsize_HeicWithFullsizeRendition_ServesIt()
    {
        var logic = CreateLogic("fullsize", request => request.RequestUri!.AbsolutePath == $"/api/assets/{_id}"
            ? AssetInfo("image/heic")
            : ImageFile("image/jpeg"));

        var result = await logic.GetAsset(_id, AssetTypeEnum.IMAGE);

        Assert.Multiple(() =>
        {
            Assert.That(_handler.Requests, Has.Count.EqualTo(2));
            Assert.That(IsThumbnail(_handler.Requests[1], "fullsize"), Is.True, _handler.Requests[1].ToString());
            Assert.That(result.ContentType, Is.EqualTo("image/jpeg"));
        });
    }

    [TestCase("preview", "image/jpeg")]
    [TestCase("original", "image/jpeg")]
    [TestCase("fullsize", "image/jpeg")]
    [TestCase("fullsize", "image/heic")]
    public async Task EveryImageRequest_AsksForTheEditedVersion(string imageSize, string originalMimeType)
    {
        // #680: without edited=true Immich ignores the crop or rotation the user applied.
        var logic = CreateLogic(imageSize, request =>
        {
            if (request.RequestUri!.AbsolutePath == $"/api/assets/{_id}") return AssetInfo(originalMimeType);
            if (IsThumbnail(request.RequestUri, "fullsize")) return Redirect("thumbnail?size=preview");
            return ImageFile("image/jpeg");
        });

        await logic.GetAsset(_id, AssetTypeEnum.IMAGE);

        var imageRequests = _handler.Requests.Where(r => r.AbsolutePath != $"/api/assets/{_id}").ToList();
        Assert.That(imageRequests, Is.Not.Empty);
        Assert.That(imageRequests.Select(r => r.Query), Has.All.Contains("edited=true"));
    }

    private bool IsThumbnail(Uri uri, string size) =>
        uri.AbsolutePath == $"/api/assets/{_id}/thumbnail" && uri.Query.Contains($"size={size}");

    private static HttpResponseMessage ImageFile(string contentType)
    {
        var content = new ByteArrayContent(Body);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static HttpResponseMessage Redirect(string location)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Found) { Content = new ByteArrayContent([]) };
        response.Headers.Location = new Uri(location, UriKind.Relative);
        return response;
    }

    private HttpResponseMessage AssetInfo(string originalMimeType)
    {
        // Built from the generated DTO so a schema change breaks the build, not the test at runtime.
        var timestamp = DateTimeOffset.Parse("2023-10-26T10:00:00Z");
        var json = JsonSerializer.Serialize(new AssetResponseDto
        {
            Id = _id,
            OwnerId = Guid.NewGuid(),
            Type = AssetTypeEnum.IMAGE,
            OriginalPath = "/path/to/image",
            OriginalFileName = "image",
            OriginalMimeType = originalMimeType,
            Checksum = "testchecksum",
            Thumbhash = "I0cMCQS94XmImZeXmYd3d3g=",
            Visibility = AssetVisibility.Timeline,
            Width = 4032,
            Height = 3024,
            HasMetadata = true,
            FileCreatedAt = timestamp,
            FileModifiedAt = timestamp,
            LocalDateTime = timestamp,
            CreatedAt = timestamp,
            UpdatedAt = timestamp,
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(respond(request));
        }
    }
}
