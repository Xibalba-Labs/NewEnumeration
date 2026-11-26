using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xibalbalabs.Enumeration.Core;

namespace Xibalbalabs.Enumeration.Serialization.Json;

/// <summary>
/// A <see cref="JsonConverterFactory"/> that automatically creates an <see cref="EnumerationJsonConverter{TEnumeration, TValue}"/>
/// for any type deriving from <see cref="Enumeration{TEnumeration, TValue}"/>.
/// </summary>
/// <remarks>
/// This factory relies on reflection (<see cref="Type.MakeGenericType(Type[])"/>) to discover the underlying value type of the
/// enumeration being converted, so it is not fully Native AOT / trimming friendly. For AOT scenarios, register
/// <see cref="EnumerationJsonConverter{TEnumeration, TValue}"/> directly for each enumeration type instead.
/// </remarks>
public sealed class EnumerationJsonConverterFactory : JsonConverterFactory
{
    private readonly EnumerationJsonSerializationFormat _format;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationJsonConverterFactory"/> class that serializes using the underlying <c>Value</c>.
    /// </summary>
    public EnumerationJsonConverterFactory()
        : this(EnumerationJsonSerializationFormat.Value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationJsonConverterFactory"/> class.
    /// </summary>
    /// <param name="format">Controls whether enumerations are written as their <c>Value</c> or their <c>DisplayName</c>.</param>
    public EnumerationJsonConverterFactory(EnumerationJsonSerializationFormat format)
    {
        _format = format;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return GetEnumerationDefinition(typeToConvert) is not null;
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var definition = GetEnumerationDefinition(typeToConvert)
            ?? throw new InvalidOperationException($"{typeToConvert} does not derive from Enumeration<,>.");

        var valueType = definition.GetGenericArguments()[1];

#pragma warning disable IL2026, IL2055, IL3050 // Building a converter for an arbitrary discovered value type inherently requires reflection; use EnumerationJsonConverter<TEnumeration, TValue> directly for Native AOT safety.
        var converterType = typeof(EnumerationJsonConverter<,>).MakeGenericType(typeToConvert, valueType);
        return (JsonConverter)Activator.CreateInstance(converterType, _format)!;
#pragma warning restore IL2026, IL2055, IL3050
    }

    private static Type? GetEnumerationDefinition(Type typeToConvert)
    {
        for (var type = typeToConvert; type is not null && type != typeof(object); type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Enumeration<,>))
            {
                return type;
            }
        }

        return null;
    }
}
