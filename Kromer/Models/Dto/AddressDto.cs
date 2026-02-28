using Kromer.Models.Entities;

namespace Kromer.Models.Dto;

public class AddressDto
{
    public string Address { get; set; }

    public decimal Balance { get; set; }

    public decimal TotalIn { get; set; }

    public decimal TotalOut { get; set; }

    public DateTime FirstSeen { get; set; }

    public int? Names { get; set; }

    public static AddressDto FromEntity(WalletEntity wallet)
    {
        return new AddressDto
        {
            Address = wallet.Address,
            Balance = wallet.Balance,
            TotalIn = wallet.TotalIn,
            TotalOut = wallet.TotalOut,
            FirstSeen = wallet.CreatedAt,
        };
    }
}