using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xibalbalabs.Enumeration.Core;

namespace Xibalbalabs.Enumeration.Serialization.Json;

/// <summary>
/// A Native AOT compatible <see cref="JsonConverter{T}"/> for <see cref="Enumeration{TEnumeration, TValue}"/> types.
/// </summary>
/// <remarks>
/// Common primitive value types (<see cref="string"/>, <see cref="int"/>, <see cref="long"/>, <see cref="short"/>,
/// <see cref="byte"/>, <see cref="double"/>, <see cref="float"/>, <see cref="decimal"/>, <see cref="bool"/> and
/// <see cref="Guid"/>) are read and written directly through <see cref="Utf8JsonReader"/>/<see cref="Utf8JsonWriter"/>
/// with no reflection involved. Any other <typeparamref name="TValue"/> falls back to <see cref="JsonSerializer"/>,
/// which relies on runtime code generation and is not guaranteed to be trimming/Native AOT safe.
/// </remarks>
/// <typeparam name="TEnumeration">The concrete enumeration type.</typeparam>
/// <typeparam name="TValue">The type of the enumeration's underlying value.</typeparam>
public sealed class EnumerationJsonConverter<
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
#endif
    TEnumeration, TValue> : JsonConverter<TEnumeration>
    where TEnumeration : Enumeration<TEnumeration, TValue>
    where TValue : IComparable
{
    private readonly EnumerationJsonSerializationFormat _format;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationJsonConverter{TEnumeration, TValue}"/> class that serializes using the underlying <c>Value</c>.
    /// </summary>
    public EnumerationJsonConverter()
        : this(EnumerationJsonSerializationFormat.Value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationJsonConverter{TEnumeration, TValue}"/> class.
    /// </summary>
    /// <param name="format">Controls whether the enumeration is written as its <c>Value</c> or its <c>DisplayName</c>.</param>
    public EnumerationJsonConverter(EnumerationJsonSerializationFormat format)
    {
        _format = format;
    }

    /// <inheritdoc />
    public override TEnumeration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (text is not null && Enumeration<TEnumeration, TValue>.TryFromName(text, out var byName, ignoreCase: true))
            {
                return byName;
            }
        }

        var value = ReadValue(ref reader, options);
        if (value is null || !Enumeration<TEnumeration, TValue>.TryFromValue(value, out var byValue))
        {
            throw new JsonException($"'{value}' is not a valid value for {typeToConvert}.");
        }

        return byValue;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnumeration value, JsonSerializerOptions options)
    {
        if (_format == EnumerationJsonSerializationFormat.DisplayName)
        {
            writer.WriteStringValue(value.DisplayName);
        }
        else
        {
            WriteValue(writer, value.Value, options);
        }
    }

    private static TValue ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (typeof(TValue) == typeof(string)) return (TValue)(object)reader.GetString()!;
        if (typeof(TValue) == typeof(int)) return (TValue)(object)reader.GetInt32();
        if (typeof(TValue) == typeof(long)) return (TValue)(object)reader.GetInt64();
        if (typeof(TValue) == typeof(short)) return (TValue)(object)reader.GetInt16();
        if (typeof(TValue) == typeof(byte)) return (TValue)(object)reader.GetByte();
        if (typeof(TValue) == typeof(double)) return (TValue)(object)reader.GetDouble();
        if (typeof(TValue) == typeof(float)) return (TValue)(object)reader.GetSingle();
        if (typeof(TValue) == typeof(decimal)) return (TValue)(object)reader.GetDecimal();
        if (typeof(TValue) == typeof(bool)) return (TValue)(object)reader.GetBoolean();
        if (typeof(TValue) == typeof(Guid)) return (TValue)(object)reader.GetGuid();

#pragma warning disable IL2026, IL3050 // Generic fallback for non-primitive TValue is not guaranteed trimming/AOT safe; primitive fast paths above cover the common cases.
        return JsonSerializer.Deserialize<TValue>(ref reader, options)!;
#pragma warning restore IL2026, IL3050
    }

    private static void WriteValue(Utf8JsonWriter writer, TValue value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case string s: writer.WriteStringValue(s); return;
            case int i: writer.WriteNumberValue(i); return;
            case long l: writer.WriteNumberValue(l); return;
            case short sh: writer.WriteNumberValue(sh); return;
            case byte b: writer.WriteNumberValue(b); return;
            case double d: writer.WriteNumberValue(d); return;
            case float f: writer.WriteNumberValue(f); return;
            case decimal m: writer.WriteNumberValue(m); return;
            case bool bo: writer.WriteBooleanValue(bo); return;
            case Guid g: writer.WriteStringValue(g); return;
        }

#pragma warning disable IL2026, IL3050 // Generic fallback for non-primitive TValue is not guaranteed trimming/AOT safe; primitive fast paths above cover the common cases.
        JsonSerializer.Serialize(writer, value, options);
#pragma warning restore IL2026, IL3050
    }
}
