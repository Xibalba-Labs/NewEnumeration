namespace Xibalbalabs.Enumeration.Serialization.Json;

/// <summary>
/// Controls how an <see cref="Xibalbalabs.Enumeration.Core.Enumeration{TEnumeration, TValue}"/> is represented in JSON.
/// </summary>
public enum EnumerationJsonSerializationFormat
{
    /// <summary>
    /// Serialize/deserialize using the enumeration's underlying <c>Value</c>. This is the default.
    /// </summary>
    Value = 0,

    /// <summary>
    /// Serialize/deserialize using the enumeration's <c>DisplayName</c> as a JSON string.
    /// </summary>
    DisplayName = 1,
}
