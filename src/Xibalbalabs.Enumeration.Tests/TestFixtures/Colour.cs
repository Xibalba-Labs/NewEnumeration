using Xibalbalabs.Enumeration.Core;

namespace Xibalbalabs.Enumeration.Tests.TestFixtures;

public sealed class Colour : Enumeration<Colour>
{
    public static readonly Colour Red = new Colour(1, "Red");
    public static readonly Colour Green = new Colour(2, "Green");
    public static readonly Colour Blue = new Colour(3, "Blue");

    private Colour(int value, string displayName)
        : base(value, displayName)
    {
    }
}
