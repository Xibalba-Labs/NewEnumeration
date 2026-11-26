using Xibalbalabs.Enumeration.Core;

namespace Xibalbalabs.Enumeration.Tests.TestFixtures;

public sealed class CountryCode : Enumeration<CountryCode, string>
{
    public static readonly CountryCode US = new CountryCode("US", "United States");
    public static readonly CountryCode CA = new CountryCode("CA", "Canada");

    private CountryCode(string value, string displayName)
        : base(value, displayName)
    {
    }
}
