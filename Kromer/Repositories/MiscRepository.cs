using Kromer.Models.Api.Krist;
using Kromer.Models.Api.Krist.Misc;
using Microsoft.AspNetCore.Mvc;

namespace Kromer.Repositories;

public class MiscRepository(IConfiguration configuration)
{
    public string GetPublicUrl()
    {
        return configuration.GetValue<string>("PublicUrl") ?? string.Empty;
    }

    public string GetPublicWsUrl()
    {
        return configuration.GetValue<string>("PublicWsUrl") ?? string.Empty;
    }

    public KristMotdResponse GetMotd()
    {
        return new KristMotdResponse
        {
            Motd = configuration.GetValue("Motd", "Welcome to Kromer."),
            Notice = configuration.GetValue("Notice", string.Empty),
            Set = DateTime.UtcNow,
            MotdSet = DateTime.UtcNow,
            PublicUrl = GetPublicUrl(),
            PublicWsUrl = GetPublicWsUrl(),
            Constants = new KristMotdResponse.MotdConstants
            {
                NameCost = GetNameCost(),
            }
        };
    }

    public int GetNameCost()
    {
        return configuration.GetValue("NameCost", 500);
    }

    public int GetWalletVersion()
    {
        return 14;
    }
}
