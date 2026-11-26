using System.Text.Json;
using Xibalbalabs.Enumeration.Serialization.Json;
using Xibalbalabs.Enumeration.Tests.TestFixtures;
using Xunit;

namespace Xibalbalabs.Enumeration.Tests.Json;

public sealed class EnumerationJsonConverterTests
{
    [Fact]
    public void Serialize_UsingValueFormat_WritesUnderlyingValue()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new EnumerationJsonConverter<Colour, int>() },
        };

        var json = JsonSerializer.Serialize(Colour.Green, options);

        Assert.Equal("2", json);
    }

    [Fact]
    public void Serialize_UsingDisplayNameFormat_WritesDisplayName()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new EnumerationJsonConverter<Colour, int>(EnumerationJsonSerializationFormat.DisplayName) },
        };

        var json = JsonSerializer.Serialize(Colour.Green, options);

        Assert.Equal("\"Green\"", json);
    }

    [Fact]
    public void Deserialize_FromUnderlyingValue_ReturnsMatchingInstance()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new EnumerationJsonConverter<Colour, int>() },
        };

        var result = JsonSerializer.Deserialize<Colour>("3", options);

        Assert.Same(Colour.Blue, result);
    }

    [Fact]
    public void Deserialize_FromDisplayNameString_ReturnsMatchingInstance()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new EnumerationJsonConverter<Colour, int>() },
        };

        var result = JsonSerializer.Deserialize<Colour>("\"Red\"", options);

        Assert.Same(Colour.Red, result);
    }

    [Fact]
    public void Deserialize_InvalidValue_ThrowsJsonException()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new EnumerationJsonConverter<Colour, int>() },
        };

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Colour>("999", options));
    }

    [Fact]
    public void StringBackedEnumeration_RoundTripsThroughValue()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new EnumerationJsonConverter<CountryCode, string>() },
        };

        var json = JsonSerializer.Serialize(CountryCode.US, options);
        var result = JsonSerializer.Deserialize<CountryCode>(json, options);

        Assert.Equal("\"US\"", json);
        Assert.Same(CountryCode.US, result);
    }

    [Fact]
    public void Factory_CanConvert_DetectsEnumerationDerivedTypes()
    {
        var factory = new EnumerationJsonConverterFactory();

        Assert.True(factory.CanConvert(typeof(Colour)));
        Assert.True(factory.CanConvert(typeof(CountryCode)));
        Assert.False(factory.CanConvert(typeof(string)));
    }

    [Fact]
    public void Factory_CreatesWorkingConverter()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new EnumerationJsonConverterFactory() },
        };

        var json = JsonSerializer.Serialize(Colour.Red, options);
        var result = JsonSerializer.Deserialize<Colour>(json, options);

        Assert.Equal("1", json);
        Assert.Same(Colour.Red, result);
    }
}
