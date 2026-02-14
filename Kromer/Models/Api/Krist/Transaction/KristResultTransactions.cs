using System.Text.Json.Serialization;
using Kromer.Models.Dto;

namespace Kromer.Models.Api.Krist.Transaction;

public class KristResultTransactions : KristResultList
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<TransactionDto> Transactions { get; set; }
}