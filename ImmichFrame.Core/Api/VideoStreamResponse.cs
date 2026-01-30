namespace ImmichFrame.Core.Api;

public sealed class VideoStreamResponse : IDisposable
{
    private readonly HttpResponseMessage _response;

    public int StatusCode { get; }
    public Stream Stream { get; }
    public string? ContentType { get; }
    public string? ContentRange { get; }
    public long? ContentLength { get; }

    public VideoStreamResponse(HttpResponseMessage response)
    {
        _response = response;
        StatusCode = (int)response.StatusCode;
        Stream = response.Content.ReadAsStream();
        ContentType = response.Content.Headers.ContentType?.ToString();
        ContentRange = response.Content.Headers.ContentRange?.ToString();
        ContentLength = response.Content.Headers.ContentLength;
    }

    public void Dispose()
    {
        Stream.Dispose();
        _response.Dispose();
    }
}
