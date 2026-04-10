using System.Text.Json.Serialization;
using Kromer.Models.Entities;

namespace Kromer.Models.Dto;

public class AddressDto
{
    [JsonIgnore]
    public int Id { get; set; }
    
    public string Address { get; set; }

    public decimal Balance { get; set; }

    [JsonPropertyName("totalin")]
    public decimal TotalIn { get; set; }

    [JsonPropertyName("totalout")] 
    public decimal TotalOut { get; set; }

    [JsonPropertyName("firstseen")]
    public DateTime FirstSeen { get; set; }

    public int? Names { get; set; }
    
    public Guid? Player { get; set; }

    public static AddressDto FromEntity(WalletEntity wallet)
    {
        return new AddressDto
        {
            Id = wallet.Id,
            Address = wallet.Address,
            Balance = wallet.Balance,
            TotalIn = wallet.TotalIn,
            TotalOut = wallet.TotalOut,
            FirstSeen = wallet.CreatedAt,
        };
    }
}