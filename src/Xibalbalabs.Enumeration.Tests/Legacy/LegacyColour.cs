#pragma warning disable CS0618 // intentionally exercising the obsolete legacy-compat shim

using Headspring;

namespace Xibalbalabs.Enumeration.Tests.TestFixtures;

public sealed class LegacyColour : Enumeration<LegacyColour>
{
    public static readonly LegacyColour Red = new LegacyColour(1, "Red");
    public static readonly LegacyColour Green = new LegacyColour(2, "Green");

    private LegacyColour(int value, string displayName)
        : base(value, displayName)
    {
    }
}
