using System.Text.Json;
using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Helpers;

public static class WebhookHelper
{
    public static async Task SendWebhookNotification(IWebhookNotification notification, string? webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl)) return;

        var httpClient = new HttpClient();

        var options = new JsonSerializerOptions
        {
            Converters = { new PolymorphicJsonConverter<IWebhookNotification>() },
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(notification, options);
        var data = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(webhookUrl, data);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Webhook successfully sent.");
        }
        else
        {
            Console.WriteLine($"Failed to send notification to webhook: {Convert.ToInt32(response.StatusCode)} {response.StatusCode}");
        }
    }
}