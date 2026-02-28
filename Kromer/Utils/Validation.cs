using System.Text.RegularExpressions;

namespace Kromer.Utils;

public static partial class Validation
{
    [GeneratedRegex(@"^k[a-z0-9]{9}$", RegexOptions.Compiled)]
    public static partial Regex V2AddressRegex();

    [GeneratedRegex(@"^(?:k[a-z0-9]{9}|[a-f0-9]{10})(?:,(?:k[a-z0-9]{9}|[a-f0-9]{10}))*$", RegexOptions.Compiled)]
    public static partial Regex AddressListRegex();

    [GeneratedRegex(@"^(?:xn--)?[a-z0-9-_]{1,64}$", RegexOptions.Compiled)]
    public static partial Regex NameFetchRegex();

    [GeneratedRegex(@"^[a-z0-9_-]{1,64}$", RegexOptions.Compiled)]
    public static partial Regex NameRegex();

    [GeneratedRegex(@"^(?:([a-z0-9-_]{1,32})@)?([a-z0-9]{1,64})\.kro$", RegexOptions.Compiled)]
    public static partial Regex MetaNameRegex();

    [GeneratedRegex(@"^[^\s.?#].[^\s]*$", RegexOptions.Compiled)]
    public static partial Regex ARecordRegex();

    public static bool IsNameValid(string name, bool fetching = false)
    {
        name = name.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name) || name.Length > 64)
        {
            return false;
        }

        return fetching
            ? NameFetchRegex().IsMatch(name)
            : NameRegex().IsMatch(name);
    }

    public static bool IsMetaNameValid(string name)
    {
        return MetaNameRegex().IsMatch(name.ToLowerInvariant());
    }

    public static MetaNameResult ParseMetaName(string name)
    {
        var match = MetaNameRegex().Match(name);
        if (!match.Success)
        {
            return new MetaNameResult
            {
                Valid = false,
            };
        }

        return new MetaNameResult
        {
            Valid = true,
            Name = match.Groups[2].Value,
            Meta = match.Groups[1].Value
        };
    }


    public static string SanitizeName(string name)
    {
        return name.Trim().ToLowerInvariant();
    }

    public static bool IsValidAddress(string address)
    {
        return V2AddressRegex().IsMatch(address);
    }

    public static bool IsValidAddressList(string addressList)
    {
        return AddressListRegex().IsMatch(addressList);
    }

    public static bool IsValidARecord(string aRecord)
    {
        return ARecordRegex().IsMatch(aRecord);
    }

    public static string StripNameSuffix(string name)
    {
        return name.ToLowerInvariant().EndsWith(".kro") ? name[..^4] : name;
    }
}