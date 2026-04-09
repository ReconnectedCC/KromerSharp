using System.Text.Json.Serialization;

namespace Kromer.Models.WebSocket;

[Flags]
public enum SubscriptionLevel : byte
{
    Blocks = 1,
    OwnBlocks = 2,
    Transactions = 4,
    OwnTransactions = 8,
    Names = 16,
    OwnNames = 32,

    [Obsolete("Unused")]
    Motd = 64,
}