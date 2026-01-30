using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Models
{
    public class AssetRequestedNotification : IWebhookNotification
    {
        public string Name { get; set; }
        public string ClientIdentifier { get; set; }
        public DateTime DateTime { get; set; }
        public Guid RequestedAssetId { get; set; }

        public AssetRequestedNotification(Guid imageId, string clientIdentifier)
        {
            Name = nameof(AssetRequestedNotification);
            ClientIdentifier = clientIdentifier;
            DateTime = DateTime.Now;
            RequestedAssetId = imageId;
        }
    }
}
