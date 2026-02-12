using Kromer.Models.Entities;

namespace Kromer.Models.Dto;

public class WalletDto
{
    public  int Id { get; set; }
    
    public string Address { get; set; }
    
    public decimal Balance { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public bool Locked { get; set; }
    
    public decimal TotalIn  { get; set; }
    
    public decimal TotalOut { get; set; }

    public static WalletDto FromEntity(WalletEntity entity)
    {
        return new WalletDto
        {
            Id = entity.Id,
            Address = entity.Address,
            Balance = entity.Balance,
            CreatedAt = entity.CreatedAt,
            Locked = entity.Locked ?? false,
            TotalIn = entity.TotalIn,
            TotalOut = entity.TotalOut,
        };
    }
}