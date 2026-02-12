using Kromer.Models.Dto;

namespace Kromer.Models.Entities;

public partial class TransactionEntity
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public string? From { get; set; }

    public string To { get; set; } = null!;

    public string? Metadata { get; set; }

    public DateTime Date { get; set; }

    public string? Name { get; set; }

    public string? SentMetaname { get; set; }

    public string? SentName { get; set; }
    
    public TransactionType TransactionType { get; set; }
}
