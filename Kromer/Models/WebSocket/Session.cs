using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Kromer.Services;

namespace Kromer.Models.WebSocket;

public class Session
{
    public const SubscriptionLevel DefaultSubscriptionLevels =
        SubscriptionLevel.Blocks | SubscriptionLevel.OwnTransactions;

    public Guid Id { get; } = Guid.CreateVersion7();

    public bool Connected { get; set; } = false;

    public DateTime InstantiatedAt { get; set; } = DateTime.UtcNow;

    public bool Authenticated => Address != null;

    public string? PrivateKey { get; set; } = null;

    [MemberNotNullWhen(true, nameof(Authenticated))]
    public string? Address { get; set; }

    public bool IsGuest => !Authenticated;
    public System.Net.WebSockets.WebSocket? WebSocket { get; set; }

    public SubscriptionLevel SubscriptionLevel { get; set; } = DefaultSubscriptionLevels;

    public async Task SendAsync<T>(T data, CancellationToken cancellationToken = default)
    {
        if (WebSocket is null || data is null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(data, data.GetType(),
            SessionManager.SessionManager.JsonSerializerOptions);
        await WebSocket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, cancellationToken);
    }
}