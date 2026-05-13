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
    public async Task SendForceTransferAlertAsync(string fromWallet, string toWallet, decimal amount, IPAddress? ipAddress, int transactionId)
    {
        if (WebhookUrl is null)
        {
            logger.LogWarning("Discord webhook URL is not set in configuration `AlertWebhook`.");
            return;
        }

        var embed = new WebhookMessage.Embed
        {
            Title = "Internal endpoint force-transfer executed!",
            Color = 0xFF0000, // Red
            Author = new WebhookMessage.Author("Kromer Alert"),
            Fields =
            [
                new WebhookMessage.Field("From Wallet", fromWallet, true),
                new WebhookMessage.Field("To Wallet", toWallet, true),
                new WebhookMessage.Field("Amount", amount.ToString("F5"), true),
                new WebhookMessage.Field("IP Address", ipAddress?.ToString() ?? "Unavailable", true),
                new WebhookMessage.Field("Transaction ID", $"[{transactionId}](https://kromer.club/transactions/{transactionId}`)", true)
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
