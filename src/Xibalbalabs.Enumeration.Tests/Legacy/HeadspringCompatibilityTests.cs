#pragma warning disable CS0618 // intentionally exercising the obsolete Headspring-compat shim

using Xibalbalabs.Enumeration.Tests.TestFixtures;
using Xunit;

namespace Xibalbalabs.Enumeration.Tests.Legacy;

/// <summary>
/// Regression tests for true drop-in compatibility with the original Headspring.Enumeration package:
/// a consumer must be able to swap the old DLL reference for this one and recompile without further
/// changes, whether their enumeration derives from the single-generic-parameter (int-backed) shorthand
/// or the fully generic two-parameter base.
/// </summary>
public sealed class HeadspringCompatibilityTests
{
    [Fact]
    public void SingleParamShim_SupportsFromInt32AndTryFromInt32()
    {
        Assert.Same(LegacyColour.Green, LegacyColour.FromInt32(2));

        var found = LegacyColour.TryFromInt32(1, out var result);

        Assert.True(found);
        Assert.Same(LegacyColour.Red, result);
    }

    [Fact]
    public void SingleParamShim_SupportsEqualityAgainstRawInt()
    {
        Assert.True(LegacyColour.Red == 1);
        Assert.True(1 == LegacyColour.Red);
        Assert.True(LegacyColour.Red != 2);
    }

    [Fact]
    public void SingleParamShim_SupportsExplicitCastToDisplayName()
    {
        var name = (string)LegacyColour.Green;

        Assert.Equal("Green", name);
    }

    [Fact]
    public void SingleParamShim_SupportsGetAllAndValueBasedEquality()
    {
        var all = LegacyColour.GetAll();

        Assert.Equal(2, all.Length);
        Assert.Equal(LegacyColour.Red, LegacyColour.FromValue(1));
    }

    [Fact]
    public void TwoParamShim_SupportsFromValueAndFromName()
    {
        Assert.Same(LegacyCountryCode.US, LegacyCountryCode.FromValue("US"));
        Assert.Same(LegacyCountryCode.CA, LegacyCountryCode.FromName("Canada"));
    }

    [Fact]
    public void TwoParamShim_ComparesToNullWithoutAmbiguity()
    {
        LegacyCountryCode? nullCountryCode = null;

        Assert.False(LegacyCountryCode.US == null);
        Assert.True(nullCountryCode == null);
        Assert.True(LegacyCountryCode.US != null);
    }

    [Fact]
    public void TwoParamShim_ExplicitCastToStringReturnsUnderlyingValue()
    {
        var value = (string)LegacyCountryCode.CA;

        Assert.Equal("CA", value);
    }
}
