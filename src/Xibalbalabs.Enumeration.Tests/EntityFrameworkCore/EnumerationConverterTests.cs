using Xibalbalabs.Enumeration.EntityFrameworkCore;
using Xibalbalabs.Enumeration.Tests.TestFixtures;
using Xunit;

namespace Xibalbalabs.Enumeration.Tests.EntityFrameworkCore;

public sealed class EnumerationConverterTests
{
    [Fact]
    public void EnumerationIdConverter_ConvertsToProviderValue()
    {
        var converter = new EnumerationIdConverter<Colour, int>();

        var providerValue = converter.ConvertToProvider(Colour.Green);

        Assert.Equal(2, providerValue);
    }

    [Fact]
    public void EnumerationIdConverter_ConvertsFromProviderValue()
    {
        var converter = new EnumerationIdConverter<Colour, int>();

        var model = converter.ConvertFromProvider(3);

        Assert.Same(Colour.Blue, model);
    }

    [Fact]
    public void EnumerationNameConverter_ConvertsToProviderValue()
    {
        var converter = new EnumerationNameConverter<Colour>();

        var providerValue = converter.ConvertToProvider(Colour.Red);

        Assert.Equal("Red", providerValue);
    }

    [Fact]
    public void EnumerationNameConverter_ConvertsFromProviderValue()
    {
        var converter = new EnumerationNameConverter<Colour>();

        var model = converter.ConvertFromProvider("Blue");

        Assert.Same(Colour.Blue, model);
    }

    [Fact]
    public void EnumerationNameConverter_SupportsStringBackedEnumerations()
    {
        var converter = new EnumerationNameConverter<CountryCode>();

        var providerValue = converter.ConvertToProvider(CountryCode.CA);
        var model = converter.ConvertFromProvider("United States");

        Assert.Equal("Canada", providerValue);
        Assert.Same(CountryCode.US, model);
    }
}
