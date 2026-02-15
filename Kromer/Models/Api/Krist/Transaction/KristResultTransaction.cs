using System.Text.Json.Serialization;
using Kromer.Models.Dto;

namespace Kromer.Models.Api.Krist.Transaction;

public class KristResultTransaction : KristResultList
{
    public TransactionDto Transaction { get; set; }
}