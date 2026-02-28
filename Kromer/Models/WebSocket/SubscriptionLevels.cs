using System.Text.Json.Serialization;

namespace Kromer.Models.WebSocket;

[Flags]
public enum SubscriptionLevel : byte
{
    [Obsolete("Unused")]
    Blocks = 1,
    
    [Obsolete("Unused")]
    OwnBlocks = 2, // Unused
    
    
    Transactions = 4,
    OwnTransactions = 8,
    Names = 16,
    OwnNames = 32,
    
    [Obsolete("Unused")]
    Motd = 64,
}