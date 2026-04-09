using Kromer.Models.Api.Krist.Transaction;
using Kromer.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.Krist;

[Route("api/krist/transactions")]
[ApiController]
public class TransactionsController(TransactionRepository transactionRepository)
    : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of transactions based on the provided parameters.
    /// </summary>
    /// <param name="limit">The maximum number of transactions to return. Defaults to 50. The value is clamped between 1 and 1000.</param>
    /// <param name="offset">The number of transactions to skip before starting to collect the return set. Defaults to 0.</param>
    /// <param name="excludeMined">A boolean indicating whether to exclude mined transactions from the result. Defaults to false.</param>
    /// <returns>An ActionResult containing a <c>KristResultTransactions</c> object with the details of the transactions.</returns>
    [HttpGet("")]
    public async Task<ActionResult<KristResultTransactions>> GetTransactions([FromQuery] int limit = 50,
        [FromQuery] int offset = 0, [FromQuery] bool excludeMined = false)
    {
        limit = Math.Clamp(limit, 1, 1000);

        var total = await transactionRepository.CountTransactionsAsync(excludeMined);
        var transactions = await transactionRepository.GetPaginatedTransactionsAsync(offset, limit, excludeMined);

        return new KristResultTransactions
        {
            Ok = true,
            Count = transactions.Count,
            Total = total,
            Transactions = transactions
        };
    }

    /// <summary>
    /// Retrieves the most recent transactions based on the provided parameters.
    /// </summary>
    /// <param name="limit">The maximum number of transactions to return. Defaults to 50. The value is clamped between 1 and 1000.</param>
    /// <param name="offset">The number of transactions to skip before starting to collect the result set. Defaults to 0.</param>
    /// <param name="excludeMined">A boolean indicating whether to exclude mined transactions from the result. Defaults to false.</param>
    /// <returns>An ActionResult containing a <c>KristResultTransactions</c> object with the details of the transactions.</returns>
    [HttpGet("latest")]
    public async Task<ActionResult<KristResultTransactions>> GetLatestTransactions([FromQuery] int limit = 50,
        [FromQuery] int offset = 0, [FromQuery] bool excludeMined = false)
    {
        limit = Math.Clamp(limit, 1, 1000);

        var total = await transactionRepository.CountTransactionsAsync(excludeMined);
        var transactions = await transactionRepository.GetPaginatedLatestTransactionsAsync(offset, limit, excludeMined);

        return new KristResultTransactions
        {
            Ok = true,
            Count = transactions.Count,
            Total = total,
            Transactions = transactions
        };
    }

    /// <summary>
    /// Retrieves the details of a specific transaction by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the transaction to retrieve.</param>
    /// <returns>An <c>ActionResult</c> containing a <c>KristResultTransaction</c> object with the transaction details.</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<KristResultTransaction>> GetTransaction(int id)
    {

        var transaction = await transactionRepository.GetTransaction(id);

        return new KristResultTransaction
        {
            Ok = true,
            Transaction = transaction,
        };
    }

    /// <summary>
    /// Initiates the creation of a new transaction based on the provided request data.
    /// </summary>
    /// <param name="request">
    /// An object containing the transaction details:
    /// <c>PrivateKey</c> - The private key of the sender's wallet (required).
    /// <c>To</c> - The address of the recipient (required).
    /// <c>Amount</c> - The amount to be transferred (required).
    /// <c>MetaData</c> - Optional metadata describing the transaction.
    /// </param>
    /// <returns>
    /// An ActionResult containing a <c>KristResultTransaction</c> object,
    /// which includes the details of the created transaction and a status indicating success or failure.
    /// </returns>
    [HttpPost("")]
    public async Task<ActionResult<KristResultTransaction>> CreateTransaction(KristRequestTransaction request)
    {

        var transaction = await transactionRepository.RequestCreateTransaction(request.PrivateKey, request.To, request.Amount, request.MetaData);

        return new KristResultTransaction
        {
            Ok = true,
            Transaction = transaction,
        };
    }
}