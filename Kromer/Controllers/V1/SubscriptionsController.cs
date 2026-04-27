using Kromer.Models.Api.V1;
using Kromer.Models.Api.V1.Subscriptions;
using Kromer.Models.Dto;
using Kromer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.V1;

[Route("api/v1/subscriptions")]
[ApiController]
public class SubscriptionsController(SubscriptionRepository subscriptionRepository) : ControllerBase
{
    [HttpPost("")]
    public async Task<ActionResult<Result<CreateSubscriptionResponse>>> CreateSubscription(
        [FromBody] CreateSubscriptionRequest? request)
    {
        return new Result<CreateSubscriptionResponse>(
            await subscriptionRepository.CreateContractAsync(request));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<Result<SubscriptionDto>>> DeleteSubscription(int id,
        [FromBody] PrivateKeyRequest? request)
    {
        return new Result<SubscriptionDto>(
            await subscriptionRepository.CancelContractAsync(id, request?.PrivateKey));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<SubscriptionDto>>> GetSubscription(int id, [FromQuery] string? address = null)
    {
        return new Result<SubscriptionDto>(
            await subscriptionRepository.GetContractAsync(id, address));
    }

    [HttpPost("{id:int}/subscribe")]
    public async Task<ActionResult<Result<SubscribeResponse>>> Subscribe(int id, [FromBody] PrivateKeyRequest? request)
    {
        return new Result<SubscribeResponse>(
            await subscriptionRepository.SubscribeAsync(id, request?.PrivateKey));
    }

    [HttpPost("{id:int}/unsubscribe")]
    public async Task<ActionResult<Result<object>>> Unsubscribe(int id, [FromBody] PrivateKeyRequest? request)
    {
        await subscriptionRepository.UnsubscribeAsync(id, request?.PrivateKey);
        return new Result<object>(new { });
    }

    [HttpGet("")]
    public async Task<ActionResult<Result<SubscriptionListResponse>>> ListSubscriptions(
        [FromQuery] string? address = null,
        [FromQuery] string? name = null,
        [FromQuery] bool excludeOwned = false,
        [FromQuery] bool onlyOwned = false,
        [FromQuery] bool onlyUnsubscribable = true,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        return new Result<SubscriptionListResponse>(
            await subscriptionRepository.ListContractsAsync(address, name, excludeOwned, onlyOwned,
                onlyUnsubscribable, limit, offset));
    }
}
