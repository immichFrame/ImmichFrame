using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.WebApi.Models
{
    public class ImageRequestedNotification : IWebhookNotification
    {
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public Guid RequestedImageId { get; set; }

        public ImageRequestedNotification(Guid imageId)
        {
            Name = nameof(ImageRequestedNotification);
            DateTime = DateTime.Now;
            RequestedImageId = imageId;
        }
    }
}
