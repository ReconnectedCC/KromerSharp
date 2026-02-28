using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Humanizer;
using Kromer.Models.Dto;
using Kromer.Models.Exceptions;
using Kromer.Models.WebSocket;
using Kromer.Models.WebSocket.Packets;
using Kromer.Models.WebSocket.Requests;
using Kromer.Models.WebSocket.Responses;
using Kromer.Repositories;

namespace Kromer.SessionManager;

public class SessionManager(ILogger<SessionManager> logger, IServiceScopeFactory scopeFactory)
{
    public static readonly TimeSpan ConnectionExpireTime = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower),
            new JsonStringEnumConverter<SubscriptionLevel>(JsonNamingPolicy.CamelCase),
        },
    };


    private static T ParseRequest<T>(string rawData) where T : KristWsRequest
    {
        try
        {
            var data = JsonSerializer.Deserialize<T>(rawData, JsonSerializerOptions);
            return data ?? throw new KristException(ErrorCode.InvalidRequestType);
        }
        catch (JsonException)
        {
            throw new KristException(ErrorCode.InvalidRequestType);
        }
    }

    private static void Assert(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new KristParameterException(name);
        }
    }

    private static void Assert(string name, decimal value)
    {
        if (value <= 0)
        {
            throw new KristParameterException(name);
        }
    }

    private static List<string> GetSubscriptionLevels(SubscriptionLevel subscriptionLevels)
    {
        var levels = Enum.GetValues<SubscriptionLevel>()
            .Where(x => subscriptionLevels.HasFlag(x))
            .Select(x => x.ToString().Camelize())
            .ToList();

        return levels;
    }

    public ICollection<Session> GetAllSessions()
    {
        return _sessions.Values.ToImmutableList();
    }

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
        if (_sessions.TryGetValue(sessionId, out session))
        {
            // Will still give you the session, but it should be discarded. 
            return session.InstantiatedAt >= DateTime.UtcNow - ConnectionExpireTime;
        }

        return false;
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

        await session.SendAsync(new KristHelloPacket());

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

        KristWsRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<KristWsRequest>(rawData, JsonSerializerOptions);
        }
        catch (JsonException)
        {
            request = null;
        }

        if (request is null)
        {
            var error = new KristWsErrorResponse
            {
                Error = "syntax_error",
                Message = "Syntax error",
            };
            await session.SendAsync(error);
            return;
        }

        IKristWsResponse response;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            response = request.Type switch
            {
                "work" => new KristWsWorkResponse(),
                "make_transaction" => await MakeTransactionAsync(scope, rawData, session),
                "get_valid_subscription_levels" => GetValidSubscriptionLevels(scope, rawData, session),
                "address" => await GetAddressAsync(scope, rawData, session),
                "me" => await GetMeAsync(scope, rawData, session),
                "get_subscription_level" => GetSubscriptionLevels(scope, rawData, session),
                "logout" => Logout(scope, rawData, session),
                "login" => await LoginAsync(scope, rawData, session),
                "subscribe" => Subscribe(scope, rawData, session),
                "unsubscribe" => Unsubscribe(scope, rawData, session),
                _ => throw new KristParameterException("type"),
            };
        }
        catch (KristParameterException ex)
        {
            response = new KristWsErrorResponse
            {
                Error = ex.Error,
                Message = ex.Message,
                Parameter = ex.Parameter,
            };
        }
        catch (KristException ex)
        {
            response = new KristWsErrorResponse()
            {
                Error = ex.Error,
                Message = ex.Message,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing WebSocket request");
            response = new KristWsErrorResponse
            {
                Error = "internal_server_error",
                Message = "Internal server error",
            };
        }

        response.Id = request.Id;
        response.RespondingToType = request.Type;
        response.Ok = response is not KristWsErrorResponse;

        await session.SendAsync(response);
    }

    private async Task<IKristWsResponse> MakeTransactionAsync(AsyncServiceScope scope, string rawData, Session session)
    {
        var request = ParseRequest<KristWsTransactionRequest>(rawData);
        if (!session.Authenticated)
        {
            Assert("privatekey", request.PrivateKey);
        }

        var privateKey = session.Authenticated ? session.PrivateKey! : request.PrivateKey!;

        var transactionRepository = scope.ServiceProvider.GetRequiredService<TransactionRepository>();
        var dto = await transactionRepository.RequestCreateTransaction(privateKey, request.To, request.Amount,
            request.MetaData);

        return new KristWsTransactionResponse
        {
            Transaction = dto
        };
    }

    private IKristWsResponse GetValidSubscriptionLevels(AsyncServiceScope scope, string rawData, Session session)
    {
        return new KristWsValidSubscriptionLevelsResponse();
    }

    private async Task<IKristWsResponse> GetAddressAsync(AsyncServiceScope scope, string rawData, Session session)
    {
        var request = ParseRequest<KristWsAddressRequest>(rawData);

        var walletRepository = scope.ServiceProvider.GetRequiredService<WalletRepository>();
        var address = await walletRepository.GetAddressAsync(request.Address);

        if (address is null)
        {
            throw new KristException(ErrorCode.AddressNotFound);
        }

        return new KristWsAddressResponse()
        {
            Address = address,
        };
    }

    private async Task<IKristWsResponse> GetMeAsync(AsyncServiceScope scope, string rawData, Session session)
    {
        var walletRepository = scope.ServiceProvider.GetRequiredService<WalletRepository>();

        var response = new KristWsMeResponse();

        if (session.Authenticated)
        {
            response.Address = await walletRepository.GetAddressAsync(session.Address!);
        }

        response.IsGuest = !session.Authenticated;

        return response;
    }

    private IKristWsResponse GetSubscriptionLevels(AsyncServiceScope scope, string rawData, Session session)
    {
        return new KristWsSubscriptionLevelResponse
        {
            SubscriptionLevel = GetSubscriptionLevels(session.SubscriptionLevel),
        };
    }

    private IKristWsResponse Logout(AsyncServiceScope scope, string rawData, Session session)
    {
        session.PrivateKey = null;
        session.Address = null;

        return new KristWsMeResponse
        {
            IsGuest = !session.Authenticated,
        };
    }

    private async Task<IKristWsResponse> LoginAsync(AsyncServiceScope scope, string rawData, Session session)
    {
        var request = ParseRequest<KristWsLoginRequest>(rawData);

        Assert("privatekey", request.PrivateKey);

        var walletRepository = scope.ServiceProvider.GetRequiredService<WalletRepository>();

        var addressResult = await walletRepository.VerifyAddressAsync(request.PrivateKey);
        if (!addressResult.Authed)
        {
            throw new KristException(ErrorCode.AuthenticationFailed);
        }

        var address = AddressDto.FromEntity(addressResult.Wallet!);

        session.PrivateKey = request.PrivateKey;
        session.Address = address.Address;

        return new KristWsMeResponse
        {
            Address = address,
            IsGuest = !session.Authenticated,
        };
    }

    public IKristWsResponse Subscribe(AsyncServiceScope scope, string rawData, Session session)
    {
        var request = ParseRequest<KristWsSubscribeRequest>(rawData);
        Assert("event", request.Event);

        if (!Enum.TryParse<SubscriptionLevel>(request.Event.Pascalize(), out var level))
        {
            throw new KristParameterException("event");
        }

        session.SubscriptionLevel |= level;

        return new KristWsSubscriptionLevelResponse
        {
            SubscriptionLevel = GetSubscriptionLevels(session.SubscriptionLevel),
        };
    }

    public IKristWsResponse Unsubscribe(AsyncServiceScope scope, string rawData, Session session)
    {
        var request = ParseRequest<KristWsSubscribeRequest>(rawData);
        Assert("event", request.Event);

        if (!Enum.TryParse<SubscriptionLevel>(request.Event.Pascalize(), out var level))
        {
            throw new KristParameterException("event");
        }

        session.SubscriptionLevel &= ~level;

        return new KristWsSubscriptionLevelResponse
        {
            SubscriptionLevel = GetSubscriptionLevels(session.SubscriptionLevel),
        };
    }
}