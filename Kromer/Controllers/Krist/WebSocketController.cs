using Kromer.Models.Api.Krist.Wallet;
using Kromer.Models.Api.Krist.WebSocket;
using Kromer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Kromer.Controllers.Krist;

[ApiController]
[Route("api/krist/ws")]
public class WebSocketController(SessionService sessionService, SessionManager.SessionManager sessionManager) : ControllerBase
{
    [HttpPost("start")]
    public async Task<ActionResult<KristResponseWebSocketInitiate>> InitConnection(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
        KristRequestOptionalPrivateKey? request = null)
    {
        var sessionId = await sessionService.InstantiateSession(request?.PrivateKey);
        var url = $"wss://{HttpContext.Request.Host}/api/krist/ws/gateway/{sessionId}";

        return new KristResponseWebSocketInitiate
        {
            Ok = true,
            Url = new Uri(url),
            Expires = (int)SessionManager.SessionManager.ConnectionExpireTime.TotalSeconds,
        };
    }

    [Route("gateway/{sessionId:guid}")]
    public async Task<ActionResult> Gateway(Guid sessionId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            return BadRequest("Invalid WebSocket request");
        }

        var session = sessionService.ValidateSession(sessionId);
        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        session.WebSocket = webSocket;
        await sessionManager.HandleWebSocketSessionAsync(session);

        return new EmptyResult();
    }
}