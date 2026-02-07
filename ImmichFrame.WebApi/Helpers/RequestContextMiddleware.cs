using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Helpers
{
    public class RequestContextMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IRequestContext requestContext)
        {
            // assetOffest
            if (context.Request.Query.TryGetValue("assetOffset", out var assetOffset))
            {
                string value = assetOffset.ToString();

                if (value != null && value.Length > 0)
                {
                    bool success = int.TryParse(value, out int number);
                    if (success)
                    {
                        requestContext.AssetOffset = number;
                    }
                    else
                    {
                        requestContext.AssetOffset = 0;
                    }
                }
                else
                {
                    requestContext.AssetOffset = 0;
                }
            }

            // assetShuffleRandom
            if (context.Request.Query.TryGetValue("assestShuffleRandom", out var assetShuffleRandom))
            {
                string value = assetShuffleRandom.ToString();

                if (value != null && value.Length > 0)
                {
                    bool success = int.TryParse(value, out int number);
                    if (success)
                    {
                        requestContext.AssetShuffleRandom = number;
                    }
                    else
                    {
                        requestContext.AssetShuffleRandom = 0;
                    }
                }
                else
                {
                    requestContext.AssetShuffleRandom = 0;
                }
            }

            await _next(context);
        }
    }
}
