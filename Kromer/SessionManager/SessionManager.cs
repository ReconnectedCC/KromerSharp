using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Kromer.Models.WebSocket;

namespace Kromer.SessionManager;

public class SessionManager(ILogger<SessionManager> logger)
{
    public static readonly TimeSpan ConnectionExpireTime = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public Session CreateSession(string? privateKey = null)
    {
        var session = new Session
        {
            PrivateKey = privateKey,
        };
        _sessions.TryAdd(session.Id, session);
        return session;
    }

    public bool TryGetSession(Guid sessionId, [MaybeNullWhen(false)] out Session session)
    {
        return _sessions.TryGetValue(sessionId, out session);
    }

    public void ExpireSession(Guid sessionId)
    {
        if (TryGetSession(sessionId, out var session) && !session.Connected)
        {
            _sessions.TryRemove(sessionId, out _);
        }
    }

    public void CleanupSessions()
    {
        var expiredSessions = _sessions
            .Where(x => !x.Value.Connected
                        && x.Value.InstantiatedAt < DateTime.UtcNow - ConnectionExpireTime)
            .Select(x => x.Key);

        foreach (var uuid in expiredSessions)
        {
            _sessions.Remove(uuid, out _);
        }
    }

    public async Task PingSessionsAsync()
    {
        var clients = _sessions.Where(x => x.Value.Connected)
            .Select(x => x.Value);
        var pingPacket = new KristKeepAlivePacket();

        await Parallel.ForEachAsync(clients, async (session, token) => { await session.SendAsync(pingPacket, token); });
    }

    public async Task HandleWebSocketSessionAsync(Session session)
    {
        var websocket = session.WebSocket;
        var buffer = new byte[4096];
        var message = new StringBuilder();
        while (websocket?.State == WebSocketState.Open)
        {
            var receiveResult = await websocket.ReceiveAsync(buffer, CancellationToken.None);
            if (receiveResult.MessageType == WebSocketMessageType.Text)
            {
                message.Append(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));
            }
            else if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                logger.LogInformation("WebSocket session {SessionId} closing", session.Id);

                session.Connected = false;
                _sessions.TryRemove(session.Id, out _);
                await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session closed",
                    CancellationToken.None);

                logger.LogInformation("WebSocket session {SessionId} closed", session.Id);
                break;
            }

            if (receiveResult.EndOfMessage)
            {
                var data = message.ToString();
                message.Clear();

                await ProcessClientMessageAsync(session, data);
            }
        }
    }

    private async Task ProcessClientMessageAsync(Session session, string rawData)
    {
        logger.LogDebug("WebSocket session {SessionId} received message: {RawData}", session.Id, rawData);
    }
}