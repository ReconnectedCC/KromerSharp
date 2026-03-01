using System.Net.WebSockets;
using System.Threading.Channels;
using Kromer.Models.WebSocket;
using Kromer.Models.WebSocket.Events;

namespace Kromer.SessionManager;

public class EventDispatcher(ILogger<EventDispatcher> logger, SessionManager sessionManager, Channel<IKristEvent> channel) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await channel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (channel.Reader.TryRead(out var kristEvent))
            {
                var sessions = sessionManager.GetAllSessions();
                await Parallel.ForEachAsync(sessions, stoppingToken, async (session, token) =>
                {
                    if (session.WebSocket?.State != WebSocketState.Open)
                    {
                        return;
                    }

                    SubscriptionLevel level = 0;
                    var address = session.Address;
                    switch (kristEvent)
                    {
                        case KristNameEvent nameEvent:
                        {
                            var isOwn = nameEvent.Name.Owner == address;
                            level = isOwn ? SubscriptionLevel.OwnNames : SubscriptionLevel.Names;
                            break;
                        }
                        case KristTransactionEvent transactionEvent:
                        {
                            var isOwn = transactionEvent.Transaction.To == address ||
                                        transactionEvent.Transaction.From == address;
                            level = isOwn ? SubscriptionLevel.OwnTransactions : SubscriptionLevel.Transactions;
                            break;
                        }
                    }

                    if (level != 0 && session.SubscriptionLevel.HasFlag(level))
                    {
                        try
                        {
                            await session.SendAsync(kristEvent, token);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to send event to session {SessionId}", session.Id);
                        }
                    }
                });
            }
        }
    }
}