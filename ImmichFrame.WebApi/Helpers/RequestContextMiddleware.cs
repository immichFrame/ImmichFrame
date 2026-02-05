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
            if (context.Request.Query.TryGetValue("assetOffset", out var assetOffset))
            {
                string value = assetOffset.ToString();
                if (value != null && value.Length > 0)
                {
                    requestContext.AssetOffset = int.Parse(value);
                }
            }

            await _next(context);
        }
    }
}
