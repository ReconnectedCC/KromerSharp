using System.Net;

namespace Kromer.Utils;

public static class LocalAddress
{
    public static bool IsLanAddress(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();

        return bytes[0] == 10 || // Class A
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || // Class B
               (bytes[0] == 192 && bytes[1] == 168) || // Class C
               bytes[0] == 127; // Loopback
    }
}