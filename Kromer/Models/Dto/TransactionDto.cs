using Kromer.Models.Entities;

namespace Kromer.Models.Dto;

public class TransactionDto
{
    public int Id { get; set; }

    public string? From { get; set; }

    public string To { get; set; }

    public decimal Value { get; set; }

    public DateTime Time { get; set; }

    public string? Name { get; set; }

    public string? Metadata { get; set; }

    public string? SentMetaname { get; set; }

    public string? SentName { get; set; }

    public TransactionType? Type { get; set; }

    public static TransactionDto FromEntity(TransactionEntity transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            From = transaction.From,
            To = transaction.To,
            Value = transaction.Amount,
            Name =  transaction.Name,
            Metadata = transaction.Metadata,
            SentMetaname = transaction.SentMetaname,
            SentName = transaction.SentName,
            Time = transaction.Date,
            Type = transaction.TransactionType,
        };
    }
}