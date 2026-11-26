#pragma warning disable CS0618 // intentionally exercising the obsolete legacy-compat shim

using Headspring;

namespace Xibalbalabs.Enumeration.Tests.TestFixtures;

public sealed class LegacyCountryCode : Enumeration<LegacyCountryCode, string>
{
    public static readonly LegacyCountryCode US = new LegacyCountryCode("US", "United States");
    public static readonly LegacyCountryCode CA = new LegacyCountryCode("CA", "Canada");

    private LegacyCountryCode(string value, string displayName)
        : base(value, displayName)
    {
    }
}
