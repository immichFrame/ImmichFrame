using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Services
{
    public class RequestContext : IRequestContext
    {
        public int AssetOffset { get; set; }
    }
}
