using System.Security.Cryptography;
using System.Text;

namespace Kromer.Utils;

public static class Crypto
{
    public static string MakeV2Address(string key, string prefix)
    {
        var protein = new byte[9];
        var used = new bool[9];
        var chain = new StringBuilder(prefix);
        var hash = Sha256(key, 2);

        for (var i = 0; i < protein.Length; i++)
        {
            protein[i] = Convert.ToByte(hash[..2], 16);
            hash = Sha256(hash, 2);
        }

        var j = 0;
        while (j < 9)
        {
            var start = 2 * j;
            var hexPart = hash.Substring(start, 2);
            var num = Convert.ToByte(hexPart, 16);
            var index = num % 9;

            if (used[index])
            {
                hash = Sha256(hash);
            }
            else
            {
                chain.Append(ToBase36(protein[index]));
                used[index] = true;
                j++;
            }
        }

        return chain.ToString();
    }

    public static string Sha256(string data, int iterations = 1)
    {
        for (var i = 0; i < iterations; i++)
        {
            var digest = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            data = Convert.ToHexStringLower(digest);
        }

        return data;
    }

    // so called "base36"
    public static char ToBase36(byte value)
    {
        var bucket = value / 7;
        var resByte = bucket switch
        {
            >= 0 and <= 9 => (byte)('0' + bucket),
            >= 10 and <= 35 => (byte)('a' + bucket - 10),
            _ => (byte)'e',
        };

        return (char)resByte;
    }

    public static string GenerateSecurePassword(int length = 32)
    {
        const string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";
        return RandomNumberGenerator.GetString(charset, length);
    }
}
