using System;
using Xibalbalabs.Enumeration.Tests.TestFixtures;
using Xunit;

namespace Xibalbalabs.Enumeration.Tests.Core;

public sealed class EnumerationTests
{
    [Fact]
    public void TryFromValue_ReturnsTrueAndInstance_WhenValueExists()
    {
        var found = Colour.TryFromValue(2, out var result);

        Assert.True(found);
        Assert.Same(Colour.Green, result);
    }

    [Fact]
    public void TryFromValue_ReturnsFalse_WhenValueDoesNotExist()
    {
        var found = Colour.TryFromValue(999, out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromName_IsCaseSensitiveByDefault()
    {
        var found = Colour.TryFromName("red", out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromName_IgnoresCase_WhenRequested()
    {
        var found = Colour.TryFromName("red", out var result, ignoreCase: true);

        Assert.True(found);
        Assert.Same(Colour.Red, result);
    }

    [Fact]
    public void FromValueOrDefault_ReturnsMatch_WhenValueExists()
    {
        var result = Colour.FromValueOrDefault(3, Colour.Red);

        Assert.Same(Colour.Blue, result);
    }

    [Fact]
    public void FromValueOrDefault_ReturnsDefault_WhenValueMissing()
    {
        var result = Colour.FromValueOrDefault(999, Colour.Red);

        Assert.Same(Colour.Red, result);
    }

    [Fact]
    public void FromNameOrDefault_ReturnsDefault_WhenNameMissing()
    {
        var result = Colour.FromNameOrDefault("Purple", Colour.Blue);

        Assert.Same(Colour.Blue, result);
    }

    [Fact]
    public void FromNameOrDefault_IgnoresCase_WhenRequested()
    {
        var result = Colour.FromNameOrDefault("green", Colour.Blue, ignoreCase: true);

        Assert.Same(Colour.Green, result);
    }

    [Fact]
    public void FromName_Throws_WhenNameIsNotDefined()
    {
        Assert.Throws<ArgumentException>(() => Colour.FromName("Purple"));
    }

    [Fact]
    public void FromDisplayName_IsEquivalentToFromName()
    {
        var result = Colour.FromDisplayName("Blue");

        Assert.Same(Colour.Blue, result);
    }

    [Fact]
    public void EqualityOperator_ComparesTwoInstancesByValue()
    {
        Colour? nullColour = null;

        Assert.True(Colour.Red == Colour.FromValue(1));
        Assert.False(Colour.Red == Colour.Green);
        Assert.True(nullColour == null);
        Assert.False(Colour.Red == nullColour);
    }

    [Fact]
    public void EqualityOperator_ComparesInstanceToUnderlyingValue()
    {
        Assert.True(Colour.Red == 1);
        Assert.True(1 == Colour.Red);
        Assert.False(Colour.Red == 2);
        Assert.True(Colour.Red != 2);
    }

    [Fact]
    public void ExplicitCast_ToValue_ReturnsUnderlyingValue()
    {
        var value = (int)Colour.Green;

        Assert.Equal(2, value);
    }

    [Fact]
    public void ExplicitCast_ToString_ReturnsDisplayName()
    {
        var name = (string)Colour.Blue;

        Assert.Equal("Blue", name);
    }

    [Fact]
    public void GetAll_ReturnsEveryDeclaredInstance()
    {
        var all = Colour.GetAll();

        Assert.Equal(3, all.Length);
        Assert.Contains(Colour.Red, all);
        Assert.Contains(Colour.Green, all);
        Assert.Contains(Colour.Blue, all);
    }

    [Fact]
    public void LegacyParse_StillWorks()
    {
        var result = Colour.Parse("Red");

        Assert.Same(Colour.Red, result);
    }

    [Fact]
    public void LegacyTryParse_ByValue_StillWorks()
    {
        var found = Colour.TryParse(2, out var result);

        Assert.True(found);
        Assert.Same(Colour.Green, result);
    }

    [Fact]
    public void StringBackedEnumeration_SupportsFromValueAndFromName()
    {
        Assert.Same(CountryCode.US, CountryCode.FromValue("US"));
        Assert.Same(CountryCode.CA, CountryCode.FromName("Canada"));
    }

    [Fact]
    public void StringBackedEnumeration_ComparesToNullWithoutAmbiguity()
    {
        // Regression test: TValue is a reference type (string) here. A naive `==`/`!=` overload against
        // TValue directly on Enumeration<TEnumeration, TValue> would make this comparison an ambiguous
        // operator compile error, since `null` would match both the Enumeration-vs-Enumeration and the
        // Enumeration-vs-TValue overloads. This must keep compiling and behaving correctly.
        CountryCode? nullCountryCode = null;

        Assert.False(CountryCode.US == null);
        Assert.True(nullCountryCode == null);
        Assert.True(CountryCode.US != null);
    }

    [Fact]
    public void StringBackedEnumeration_ExplicitCastToStringReturnsUnderlyingValue()
    {
        // Regression test: when TValue is itself string, a second `explicit operator string` for
        // DisplayName would collide with the cast to the underlying TValue (same source/target types),
        // which is a compile error. The single available (string) cast must return Value.
        var value = (string)CountryCode.CA;

        Assert.Equal("CA", value);
    }
}
