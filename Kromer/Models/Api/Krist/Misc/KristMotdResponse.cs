using System.Diagnostics;
using Kromer.Utils;

namespace Kromer.Models.Api.Krist.Misc;

public class KristMotdResponse : KristResult
{
    public new bool Ok { get; set; } = true;

    public DateTime ServerTime { get; set; } = DateTime.UtcNow;

    public string Motd { get; set; }

    public DateTime? Set { get; set; } = null;

    public DateTime? MotdSet { get; set; } = null;

    public string PublicUrl { get; set; }

    public string PublicWsUrl { get; set; }

    // Fuck you
    public bool MiningEnabled { get; set; } = false;

    // Disabling not supported.
    public bool TransactionsEnabled { get; set; } = true;

    public bool DebugMode { get; set; } = Debugger.IsAttached;

    public int Work { get; set; } = 500;

    public object? LastBlock { get; set; } = null;

    public MotdPackage Package { get; set; } = new MotdPackage();
    
    public MotdConstants Constants { get; set; } = new MotdConstants();

    public MotdCurrency Currency { get; set; } = new MotdCurrency();

    public class MotdPackage
    {
        public string Name { get; set; } = "Kromer";

        public string Version { get; set; } = "1.0.0";

        public string Author { get; set; } = "ReconnectedCC Team";

        public string Licence { get; set; } = "GPL-3.0";

        public string Repository { get; set; } = "https://github.com/ReconnectedCC/KromerSharp";

        public string GitHash { get; set; } = Crypto.Sha256("swono qomp");
    }

    public class MotdConstants
    {
        public int WalletVersion { get; set; } = 3;
        
        public int NonceMaxSize { get; set; } = 500;

        public int NameCost { get; set; } = 500;

        public int MinWork { get; set; } = 50;
        
        public int MaxWork { get; set; } = 500;

        public decimal WorkFactor { get; set; } = 500m;

        public int SecondsPerBlock { get; set; } = 500;
    }

    public class MotdCurrency
    {
        public string AddressPrefix { get; set; } = "k";
        
        public string NameSuffix { get; set; } = "kro";

        public string CurrencyName { get; set; } = "Kromer";
        
        public string CurrencySymbol { get; set; } = "KRO";
    }
}