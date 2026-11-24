using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ImmichFrame.Core.Helpers;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace ImmichFrame.WebApi.Controllers;

[ApiController, Authorize, Route("api/gallery")]
public sealed class AlbumGalleryController(IServerSettings settings, IHttpClientFactory clients,
    IMemoryCache cache, SettingsServiceOptions options, IDataProtectionProvider protection) : Controller
{
    public record Asset(Guid Id, string Type, string OriginalFileName, DateTimeOffset FileCreatedAt);
    public record Album(string AlbumName, string? Order, Asset[] Assets);
    public record Photo(string Key, string Name);
    public record Transform(double X, double Y, double Width, double Height, int Turns, bool FlipX, bool FlipY);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private IEnumerable<(string Id, IAccountSettings Settings)> Accounts =>
        settings.Accounts.Select(a => (new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(
            a.ImmichServerUrl.TrimEnd('/') + "\n" + a.ApiKey))[..16]).ToString(), a));

    private IAccountSettings Account(string id, Guid album) =>
        Accounts.FirstOrDefault(a => a.Id == id && a.Settings.Albums.Contains(album)).Settings
        ?? throw new BadHttpRequestException("Album is outside the configured gallery.", 404);

    private HttpClient Client(IAccountSettings a)
    {
        var client = clients.CreateClient(ImmichApiHttpClientExtensions.ImmichApiAccountClient);
        client.BaseAddress = new Uri(a.ImmichServerUrl.TrimEnd('/') + "/api/");
        client.DefaultRequestHeaders.Add("x-api-key", a.ApiKey);
        return client;
    }

    private async Task<Album> ReadAlbum(string account, Guid album, CancellationToken ct)
    {
        var a = Account(account, album);
        return (await cache.GetOrCreateAsync($"gallery:{account}/{album}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5);
            using var client = Client(a);
            return await client.GetFromJsonAsync<Album>($"albums/{album}", ct);
        })) ?? throw new BadHttpRequestException("Invalid album response.", 502);
    }

    private async Task RequirePhoto(string account, Guid album, Guid asset, CancellationToken ct)
    {
        var info = await ReadAlbum(account, album, ct);
        if (!info.Assets.Any(item => item.Id == asset && item.Type.Equals("IMAGE", StringComparison.OrdinalIgnoreCase)))
            throw new BadHttpRequestException("Photo is outside the selected album.", 404);
    }

    // Keep upstream URLs, credentials and response bodies out of client errors.
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();
        if (result.Exception is HttpRequestException upstream)
        {
            result.ExceptionHandled = true;
            result.Result = StatusCode(upstream.StatusCode == HttpStatusCode.Forbidden ? 403 : 502,
                upstream.StatusCode == HttpStatusCode.Forbidden ? "Immich denied this operation. Check account and API key permissions." : "Immich request failed.");
        }
        else if (result.Exception is BadHttpRequestException invalid)
        {
            result.ExceptionHandled = true;
            result.Result = StatusCode(invalid.StatusCode, invalid.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Gallery(int offset = 0, int limit = 48, string? from = null, int step = 0, CancellationToken ct = default)
    {
        var albums = new List<object>();
        var photos = new List<Photo>();
        var seen = new HashSet<(string, Guid)>();
        foreach (var (account, source) in Accounts)
        foreach (var album in source.Albums.Distinct())
        {
            var info = await ReadAlbum(account, album, ct);
            albums.Add(new { account, album, name = info.AlbumName });
            var ordered = string.Equals(info.Order, "asc", StringComparison.OrdinalIgnoreCase)
                ? info.Assets.OrderBy(a => a.FileCreatedAt) : info.Assets.OrderByDescending(a => a.FileCreatedAt);
            foreach (var item in ordered.ThenBy(a => a.Id))
                if (item.Type.Equals("IMAGE", StringComparison.OrdinalIgnoreCase) && seen.Add((account, item.Id)))
                    photos.Add(new($"{account}/{album}/{item.Id}", item.OriginalFileName));
        }
        if (from != null)
        {
            var index = photos.FindIndex(p => p.Key == from);
            if (photos.Count == 0) return NoContent();
            if (step == 0 && index < 0) return NotFound();
            offset = (Math.Max(0, index + Math.Clamp(step, -1, 1)) + photos.Count) % photos.Count;
            if (index == 0 && step < 0) offset = photos.Count - 1;
            limit = 1;
        }
        return Ok(new { albums, total = photos.Count, photos = photos.Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 100)) });
    }

    [HttpGet("{account}/{album:guid}/{asset:guid}/image")]
    public async Task<IActionResult> Image(string account, Guid album, Guid asset, bool thumbnail, CancellationToken ct)
    {
        await RequirePhoto(account, album, asset, ct);
        using var client = Client(Account(account, album));
        var response = await client.GetAsync($"assets/{asset}/thumbnail?size={(thumbnail ? "thumbnail" : "preview")}", HttpCompletionOption.ResponseHeadersRead, ct);
        HttpContext.Response.RegisterForDispose(response);
        response.EnsureSuccessStatusCode();
        return File(await response.Content.ReadAsStreamAsync(ct), response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
    }

    private string CropPath(Guid frame, string account, Guid asset) =>
        Path.Combine(options.ConfigPath, "frame-crops", frame.ToString(), account, asset + ".json");

    [HttpGet("{account}/{album:guid}/{asset:guid}/crop/{frame:guid}")]
    public async Task<IActionResult> Crop(string account, Guid album, Guid asset, Guid frame, CancellationToken ct)
    {
        await RequirePhoto(account, album, asset, ct);
        var path = CropPath(frame, account, asset);
        try { return Content(await System.IO.File.ReadAllTextAsync(path, ct), "application/json"); }
        catch (IOException) when (!System.IO.File.Exists(path)) { return Ok(new Transform(0,0,1,1,0,false,false)); }
    }

    [HttpPut("{account}/{album:guid}/{asset:guid}/crop/{frame:guid}")]
    public async Task<IActionResult> Save(string account, Guid album, Guid asset, Guid frame, Transform transform, CancellationToken ct)
    {
        await RequirePhoto(account, album, asset, ct);
        if (transform.X < 0 || transform.Y < 0 || transform.Width <= 0 || transform.Height <= 0 ||
            transform.X + transform.Width > 1.000001 || transform.Y + transform.Height > 1.000001 || transform.Turns is < 0 or > 3)
            return BadRequest("Crop is outside the image.");
        var path = CropPath(frame, account, asset);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + Guid.NewGuid() + ".tmp";
        try {
            await System.IO.File.WriteAllTextAsync(temp, JsonSerializer.Serialize(transform, Json), ct);
            System.IO.File.Move(temp, path, true);
        } finally { System.IO.File.Delete(temp); }
        return NoContent();
    }

    private IDataProtector Receipts => protection.CreateProtector("ImmichFrame.GalleryUpload.v1");

    [HttpPost("{account}/{album:guid}/upload")]
    [RequestSizeLimit(65_000_000)]
    public async Task<IActionResult> Upload(string account, Guid album, IFormFile file, CancellationToken ct)
    {
        if (!Request.Headers.ContainsKey("X-Frame-Upload")) return BadRequest();
        using var client = Client(Account(account, album));
        await using var stream = file.OpenReadStream();
        byte[] prefix = new byte[12];
        if (file.Length < 12) return BadRequest("Empty or incomplete image.");
        await stream.ReadExactlyAsync(prefix, ct);
        bool image = prefix.AsSpan().StartsWith(new byte[]{255,216,255}) ||
            prefix.AsSpan().StartsWith(new byte[]{137,80,78,71,13,10,26,10}) ||
            prefix.AsSpan().StartsWith("GIF89a"u8) || prefix.AsSpan().StartsWith("GIF87a"u8) ||
            prefix.AsSpan().StartsWith("RIFF"u8) && prefix.AsSpan(8).SequenceEqual("WEBP"u8);
        if (!image) return BadRequest("Choose a JPEG, PNG, GIF or WebP image.");
        stream.Position = 0;
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
        stream.Position = 0;
        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(stream), "assetData", Path.GetFileName(file.FileName));
        var now = DateTimeOffset.UtcNow.ToString("O");
        foreach (var (key, value) in new[] { ("deviceAssetId",hash), ("deviceId","ImmichFrame"), ("fileCreatedAt",now), ("fileModifiedAt",now) })
            form.Add(new StringContent(value), key);
        using var response = await client.PostAsync("assets", form, ct);
        response.EnsureSuccessStatusCode();
        var asset = (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();
        return Ok(new { asset, receipt = Receipts.Protect($"{account}/{album}/{asset}") });
    }

    [HttpPost("{account}/{album:guid}/attach/{asset:guid}")]
    public async Task<IActionResult> Attach(string account, Guid album, Guid asset, [FromBody] string receipt, CancellationToken ct)
    {
        try { if (Receipts.Unprotect(receipt) != $"{account}/{album}/{asset}") return BadRequest(); }
        catch (CryptographicException) { return BadRequest(); }
        using var client = Client(Account(account, album));
        using var response = await client.PutAsJsonAsync($"albums/{album}/assets", new { ids = new[]{asset} }, ct);
        response.EnsureSuccessStatusCode();
        var rows = await response.Content.ReadFromJsonAsync<JsonElement[]>(ct);
        if (rows is not {Length:1} || (!rows[0].GetProperty("success").GetBoolean() &&
            !(rows[0].TryGetProperty("error", out var error) && error.GetString() == "duplicate")))
            return StatusCode(409,"Uploaded. Adding to the album failed.");
        cache.Remove($"gallery:{account}/{album}");
        return NoContent();
    }
}
