using ImmichFrame.Core.Api;
using ImmichFrame.Core.Exceptions;
using ImmichFrame.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[assembly: HostingStartup(typeof(ImmichFrame.WebApi.Services.TranscoderStartup))]

namespace ImmichFrame.WebApi.Services;

/// <summary>
/// Registers the optional sidecar without changing the existing application
/// startup path. The filter runs after authorization and before GetAsset.
/// </summary>
public sealed class TranscoderStartup : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ITranscoderResolver, TranscoderResolver>();
            services.AddScoped<TranscodedVideoFilter>();
            services.Configure<MvcOptions>(options => options.Filters.AddService<TranscodedVideoFilter>());
        });
    }
}

public sealed class TranscodedVideoFilter(
    ITranscoderResolver resolver,
    IImmichFrameLogic logic,
    ILogger<TranscodedVideoFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller.GetType().Name != "AssetController" ||
            !string.Equals(context.ActionDescriptor.RouteValues["action"], "GetAsset", StringComparison.Ordinal) ||
            !TryGetVideoAsset(context, out var assetId))
        {
            await next();
            return;
        }

        AssetResponseDto asset;
        try
        {
            asset = await logic.GetAssetInfoById(assetId);
        }
        catch (AssetNotFoundException)
        {
            // The client may still have a video URL cached after ShowVideos was
            // disabled or the asset left the active selection. The controller
            // cannot serve it either, so return a normal missing-resource response.
            logger.LogDebug("Video {AssetId} is no longer available", assetId);
            context.Result = new NotFoundResult();
            return;
        }

        logger.LogInformation("Video {AssetId} requested; asking transcoder", assetId);
        var resolution = await resolver.ResolveAsync(assetId, asset.Checksum, context.HttpContext.RequestAborted);
        logger.LogInformation("Video {AssetId} transcoder resolution: {Resolution}", assetId, resolution.Kind);
        if (resolution.Kind == TranscoderResolutionKind.Ready && resolution.FilePath != null)
        {
            logger.LogInformation("Video {AssetId} served from transcoded file {FilePath}", assetId, resolution.FilePath);
            context.Result = new PhysicalFileResult(resolution.FilePath, "video/mp4") { EnableRangeProcessing = true };
            return;
        }

        if (resolution.Kind == TranscoderResolutionKind.Pending)
        {
            logger.LogInformation("Video {AssetId} is queued or being transcoded; returning 503 with Retry-After", assetId);
            context.HttpContext.Response.Headers.RetryAfter = "30";
            context.Result = new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            return;
        }

        logger.LogInformation("Video {AssetId} will be served directly by Immich ({Resolution})", assetId, resolution.Kind);
        await next();
    }

    private static bool TryGetVideoAsset(ActionExecutingContext context, out Guid assetId)
    {
        assetId = default;
        if (!context.ActionArguments.TryGetValue("id", out var rawId) || rawId is not Guid id)
            return false;

        // Generated client URLs send assetType=VIDEO. Numeric is accepted for
        // callers that serialize the generated enum as its underlying value.
        var type = context.HttpContext.Request.Query["assetType"].ToString();
        if (!string.Equals(type, "VIDEO", StringComparison.OrdinalIgnoreCase) && type != ((int)AssetTypeEnum.VIDEO).ToString())
            return false;

        assetId = id;
        return true;
    }
}
