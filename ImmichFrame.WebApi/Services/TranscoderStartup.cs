using ImmichFrame.Core.Api;
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
    IImmichFrameLogic logic) : IAsyncActionFilter
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

        var asset = await logic.GetAssetInfoById(assetId);
        var resolution = await resolver.ResolveAsync(assetId, asset.Checksum, context.HttpContext.RequestAborted);
        if (resolution.Kind == TranscoderResolutionKind.Ready && resolution.FilePath != null)
        {
            context.Result = new PhysicalFileResult(resolution.FilePath, "video/mp4") { EnableRangeProcessing = true };
            return;
        }

        if (resolution.Kind == TranscoderResolutionKind.Pending)
        {
            context.HttpContext.Response.Headers.RetryAfter = "30";
            context.Result = new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            return;
        }

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
