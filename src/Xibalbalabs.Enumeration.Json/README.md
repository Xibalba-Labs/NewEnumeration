# Xibalbalabs.Enumeration.Json

`System.Text.Json` support for [Xibalbalabs.Enumeration](https://www.nuget.org/packages/Xibalbalabs.Enumeration) smart enums. Serializes an enumeration as its underlying value (default) or its display name, and reads both forms back into the matching instance.

## Installation

```powershell
dotnet add package Xibalbalabs.Enumeration.Json
```

Targets `netstandard2.0`, `net8.0` and `net9.0`.

## Usage

Register the factory once and every enumeration is handled automatically:

```csharp
using System.Text.Json;
using Xibalbalabs.Enumeration.Serialization.Json;

var options = new JsonSerializerOptions
{
    Converters = { new EnumerationJsonConverterFactory() }
};

JsonSerializer.Serialize(OrderStatus.Shipped, options);   // 2
JsonSerializer.Deserialize<OrderStatus>("2", options);    // OrderStatus.Shipped
```

Write display names instead of values:

```csharp
new EnumerationJsonConverterFactory(EnumerationJsonSerializationFormat.DisplayName);
// OrderStatus.Shipped -> "Shipped"
```

## Reading rules

- A JSON string is matched against `DisplayName` first (case-insensitive).
- Otherwise the token is read as the underlying value type and matched against `Value`.
- No match throws `JsonException`.

## Native AOT

`EnumerationJsonConverterFactory` uses reflection and is not fully AOT friendly. In AOT apps register the concrete converter per type, which reads and writes common primitive values (`string`, `int`, `long`, `short`, `byte`, `double`, `float`, `decimal`, `bool`, `Guid`) without reflection:

```csharp
options.Converters.Add(new EnumerationJsonConverter<OrderStatus, int>());
```

## License

MIT
