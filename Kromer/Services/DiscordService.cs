using System.Net;
using Kromer.Models;

namespace Kromer.Services;

public class DiscordService(IConfiguration configuration, ILogger<DiscordService> logger)
{
    private readonly HttpClient _httpClient = new();

    public Uri? WebhookUrl => configuration.GetValue<Uri?>("AlertWebhook", null);

    public async Task SendGiveMoneyAlertAsync(string walletAddress, decimal amount, IPAddress ipAddress)
    {
        if (WebhookUrl is null)
        {
            logger.LogWarning("Discord webhook URL is not set in configuration `AlertWebhook`.");
            return;
        }

        var embed = new WebhookMessage.Embed
        {
            Title = "Internal endpoint give-money executed from external address!",
            Color = 0xFF0000, // Red
            Author = new WebhookMessage.Author("Kromer Alert"),
            Fields =
            [
                new WebhookMessage.Field("Wallet Address", walletAddress, true),
                new WebhookMessage.Field("Amount", amount.ToString("F5"), true),
                new WebhookMessage.Field("Remote IP Address", ipAddress.ToString(), false),
            ]
        };

        var message = new WebhookMessage
        {
            Embeds = [embed],
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(WebhookUrl, message);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to send Discord webhook message: {StatusCode} {Content}", response.StatusCode,
                    await response.Content.ReadAsStringAsync());
            }
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Failed to send Discord webhook message");
        }
    }
}