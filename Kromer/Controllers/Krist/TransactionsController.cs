using Kromer.Models.Api.Krist.Transaction;
using Kromer.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.Krist;

[Route("api/krist/[controller]")]
[ApiController]
public class TransactionsController(TransactionRepository transactionRepository, WalletRepository walletRepository)
    : ControllerBase
{
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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<KristResultTransaction>> GetLatestTransactions(int id)
    {

        var transaction = await transactionRepository.GetTransaction(id);

        return new KristResultTransaction
        {
            Ok = true,
            Transaction = transaction,
        };
    }

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