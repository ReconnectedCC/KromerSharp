using Kromer.Models.Api.V1;
using Kromer.Models.Api.V1.Subscriptions;
using Kromer.Models.Dto;
using Kromer.Models.Exceptions;
using Kromer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.V1;

[Route("api/v1/subscriptions")]
[ApiController]
public class SubscriptionsController(SubscriptionRepository subscriptionRepository) : ControllerBase
{
    /// <summary>
    /// Creates a subscription contract for the supplied name or metaname.
    /// </summary>
    /// <param name="request">The contract details and private key of the current name owner.</param>
    /// <returns>The identifier of the created subscription contract.</returns>
    /// <exception cref="KromerException">Thrown when the request is invalid, authentication fails, or the name does not exist.</exception>
    [HttpPost("")]
    public async Task<ActionResult<Result<CreateSubscriptionResponse>>> CreateSubscription(
        [FromBody] CreateSubscriptionRequest? request)
    {
        return new Result<CreateSubscriptionResponse>(
            await subscriptionRepository.CreateContractAsync(request));
    }

    /// <summary>
    /// Cancels a subscription contract and all active wallet subscriptions attached to it.
    /// </summary>
    /// <param name="id">The identifier of the subscription contract to cancel.</param>
    /// <param name="request">The private key of the current name owner.</param>
    /// <returns>The cancelled subscription contract.</returns>
    /// <exception cref="KromerException">Thrown when the contract is not found, the private key is invalid, or the caller does not own the name.</exception>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<Result<SubscriptionDto>>> DeleteSubscription(int id,
        [FromBody] PrivateKeyRequest? request)
    {
        return new Result<SubscriptionDto>(
            await subscriptionRepository.CancelContractAsync(id, request?.PrivateKey));
    }

    /// <summary>
    /// Closes a subscription contract to new subscribers while keeping existing subscriptions active.
    /// </summary>
    /// <param name="id">The identifier of the subscription contract to close.</param>
    /// <param name="request">The private key of the current name owner.</param>
    /// <returns>The closed subscription contract.</returns>
    /// <exception cref="KromerException">Thrown when the contract is not found, the private key is invalid, or the caller does not own the name.</exception>
    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<Result<SubscriptionDto>>> CloseSubscription(int id,
        [FromBody] PrivateKeyRequest? request)
    {
        return new Result<SubscriptionDto>(
            await subscriptionRepository.CloseContractAsync(id, request?.PrivateKey));
    }

    /// <summary>
    /// Retrieves a subscription contract by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the subscription contract to retrieve.</param>
    /// <param name="addresses">Optional wallet addresses used to include caller-specific subscription state.</param>
    /// <returns>The subscription contract details.</returns>
    /// <exception cref="KromerException">Thrown when the contract is not found or the address is invalid.</exception>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<SubscriptionDto>>> GetSubscription(int id,
        [FromQuery(Name = "address")] List<string>? addresses = null)
    {
        return new Result<SubscriptionDto>(
            await subscriptionRepository.GetContractAsync(id, addresses));
    }

    /// <summary>
    /// Subscribes the authenticated wallet to a subscription contract.
    /// </summary>
    /// <param name="id">The identifier of the subscription contract to subscribe to.</param>
    /// <param name="request">The private key of the subscribing wallet.</param>
    /// <returns>The next payment date for the wallet subscription.</returns>
    /// <exception cref="KromerException">Thrown when authentication fails or the wallet cannot subscribe to the contract.</exception>
    [HttpPost("{id:int}/subscribe")]
    public async Task<ActionResult<Result<SubscribeResponse>>> Subscribe(int id, [FromBody] PrivateKeyRequest? request)
    {
        return new Result<SubscribeResponse>(
            await subscriptionRepository.SubscribeAsync(id, request?.PrivateKey));
    }

    /// <summary>
    /// Unsubscribes the authenticated wallet from a subscription contract.
    /// </summary>
    /// <param name="id">The identifier of the subscription contract to unsubscribe from.</param>
    /// <param name="request">The private key of the subscribed wallet.</param>
    /// <returns>An empty result when the unsubscribe request has been processed.</returns>
    /// <exception cref="KromerException">Thrown when authentication fails, the contract is not found, or the subscription cannot be unsubscribed.</exception>
    [HttpPost("{id:int}/unsubscribe")]
    public async Task<ActionResult<Result<object>>> Unsubscribe(int id, [FromBody] PrivateKeyRequest? request)
    {
        await subscriptionRepository.UnsubscribeAsync(id, request?.PrivateKey);
        return new Result<object>(new { });
    }

    /// <summary>
    /// Lists subscription contracts related to a wallet address or name.
    /// </summary>
    /// <param name="addresses">The wallet addresses used to find owned and subscribed contracts.</param>
    /// <param name="names">The names or metanames used to filter contracts by receiver.</param>
    /// <param name="excludeOwned">Excludes contracts owned by the supplied addresses when set.</param>
    /// <param name="onlyOwned">Only includes contracts owned by the supplied addresses when set.</param>
    /// <param name="onlyUnsubscribable">Only includes active wallet subscriptions that can currently be unsubscribed.</param>
    /// <param name="limit">The maximum number of contracts to return. The value is between 1 and 1000!</param>
    /// <param name="offset">The number of contracts to skip before returning results.</param>
    /// <returns>A paginated list of subscription contracts.</returns>
    /// <exception cref="KromerException">Thrown when neither address nor name is supplied, or when supplied filters are invalid.</exception>
    [HttpGet("")]
    public async Task<ActionResult<Result<SubscriptionListResponse>>> ListSubscriptions(
        [FromQuery(Name = "address")] List<string>? addresses = null,
        [FromQuery(Name = "name")] List<string>? names = null,
        [FromQuery(Name = "exclude_owned")] bool excludeOwned = false,
        [FromQuery(Name = "only_owned")] bool onlyOwned = false,
        [FromQuery(Name = "only_unsubscribable")] bool onlyUnsubscribable = true,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        return new Result<SubscriptionListResponse>(
            await subscriptionRepository.ListContractsAsync(addresses, names, excludeOwned, onlyOwned,
                onlyUnsubscribable, limit, offset));
    }
}
