using System.Threading.Channels;
using Kromer.Models.Exceptions;
using Kromer.Models.WebSocket;
using Kromer.Models.WebSocket.Events;
using Kromer.Repositories;

namespace Kromer.Services;

public class SessionService(
    SessionManager.SessionManager sessionManager,
    WalletRepository walletRepository,
    Channel<IKristEvent> channel,
    ILogger<SessionService> logger)
{
    public async Task<Guid> InstantiateSession(string? privateKey = null)
    {
        var session = sessionManager.CreateSession(privateKey);

        if (string.IsNullOrEmpty(privateKey))
        {
            return session.Id;
        }

        var result = await walletRepository.VerifyAddressAsync(privateKey);

        if (!result.Authed)
        {
            sessionManager.ExpireSession(session.Id);
            throw new KristException(ErrorCode.AuthenticationFailed);
        }

        session.Address = result.Wallet!.Address;

        return session.Id;
    }

    public Session ValidateSession(Guid sessionId)
    {
        if (!sessionManager.TryGetSession(sessionId, out var session))
        {
            throw new KristException(ErrorCode.InvalidWebsocketToken);
        }

        session.Connected = true;
        logger.LogInformation("WebSocket session {SessionId} connected", sessionId);

        return session;
    }
}