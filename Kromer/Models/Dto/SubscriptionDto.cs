using System.Text.Json.Serialization;
using Kromer.Models.Entities;

namespace Kromer.Models.Dto;

public class SubscriptionDto
{
    public int Id { get; set; }

    public string Description { get; set; } = null!;

    public decimal Price { get; set; }

    public int Period { get; set; }

    public string Name { get; set; } = null!;

    public int Subscribers { get; set; }

    public SubscriptionStatus Status { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Subscribed { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Owns { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? NextPayment { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Unsubscribable { get; set; }
}
