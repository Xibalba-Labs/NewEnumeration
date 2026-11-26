# Xibalbalabs.Enumeration.Json

`System.Text.Json` support for `Xibalbalabs.Enumeration` smart enums. It serializes an enumeration as either its **underlying value** (default) or its **display name**, and deserializes both forms back into the matching singleton instance.

## Installation

```powershell
dotnet add package Xibalbalabs.Enumeration.Json
```

This also brings in the core package, `Xibalbalabs.Enumeration`.

| Target framework | Notes |
|---|---|
| `netstandard2.0` | Adds `System.Text.Json` |
| `net8.0`, `net9.0` | AOT compatible (see [Native AOT](#native-aot-and-trimming)) |

## Components

| Type | Purpose |
|---|---|
| `EnumerationJsonConverter<TEnumeration, TValue>` | Converter for one specific enumeration type. Native AOT friendly. |
| `EnumerationJsonConverterFactory` | Detects any type deriving from `Enumeration<,>` and creates the right converter automatically. Uses reflection. |
| `EnumerationJsonSerializationFormat` | `Value` (default) or `DisplayName`. |

## Quick start

Given an enumeration:

```csharp
using Xibalbalabs.Enumeration.Core;

public sealed class OrderStatus : Enumeration<OrderStatus>
{
    public static readonly OrderStatus Pending = new(1, "Pending");
    public static readonly OrderStatus Shipped = new(2, "Shipped");

    private OrderStatus(int value, string displayName) : base(value, displayName) { }
}
```

Register the factory once and every enumeration in your model is handled:

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

Note the namespace: `Xibalbalabs.Enumeration.Serialization.Json`.

Enumerations work as properties too:

```csharp
public sealed record OrderDto(int Id, OrderStatus Status);

JsonSerializer.Serialize(new OrderDto(7, OrderStatus.Pending), options);
// {"Id":7,"Status":1}
```

## Serialization formats

### Value (default)

Writes the underlying `Value` using the matching JSON type.

| Enumeration | JSON |
|---|---|
| `OrderStatus.Shipped` (`int` 2) | `2` |
| `CountryCode.US` (`string` "US") | `"US"` |

### Display name

Writes the `DisplayName` as a JSON string.

```csharp
var options = new JsonSerializerOptions
{
    Converters =
    {
        new EnumerationJsonConverterFactory(EnumerationJsonSerializationFormat.DisplayName)
    }
};

JsonSerializer.Serialize(OrderStatus.Shipped, options);   // "Shipped"
```

## Deserialization rules

Reading is **lenient** and independent of the configured write format:

1. If the JSON token is a **string**, it is first matched against `DisplayName`, **ignoring case** (`"shipped"` works).
2. Otherwise (or if no name matched), the token is read as the underlying value type and matched against `Value`.
3. If nothing matches, a `JsonException` is thrown: `'999' is not a valid value for OrderStatus.`

| Enumeration | JSON input | Result |
|---|---|---|
| `OrderStatus` (`int`) | `2` | `Shipped` |
| `OrderStatus` (`int`) | `"Shipped"` / `"shipped"` | `Shipped` |
| `OrderStatus` (`int`) | `999` | `JsonException` |
| `CountryCode` (`string`) | `"US"` | `US` (by value) |
| `CountryCode` (`string`) | `"united states"` | `US` (by name, case-insensitive) |

Because of this, a service can emit values while accepting names from clients (or the reverse) without extra configuration.

`null` JSON tokens for reference-type properties are handled by `System.Text.Json` itself and yield `null`; declare the property as nullable (`OrderStatus?`).

## Supported underlying value types

The following `TValue` types are read and written directly with `Utf8JsonReader` / `Utf8JsonWriter`, with no reflection:

`string`, `int`, `long`, `short`, `byte`, `double`, `float`, `decimal`, `bool`, `Guid`.

Any other `TValue` falls back to `JsonSerializer` for the value, which is **not** guaranteed to be trimming or AOT safe.

## ASP.NET Core

### Minimal APIs

```csharp
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new EnumerationJsonConverterFactory()));
```

### MVC controllers

```csharp
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new EnumerationJsonConverterFactory()));
```

Request bodies and responses that contain enumeration properties are then converted automatically. Combine with `[EnumerationDefined]` from `Xibalbalabs.Enumeration.AspNetCore` if you bind raw values and want a validation error instead of a deserialization error.

## Per-type control with attributes

To fix the format for one enumeration regardless of global options, annotate the type with the concrete converter:

```csharp
[JsonConverter(typeof(EnumerationJsonConverter<OrderStatus, int>))]
public sealed class OrderStatus : Enumeration<OrderStatus>
{
    // ...
}
```

The attribute constructor requires a converter with a parameterless constructor, which yields the default `Value` format. For `DisplayName` on a single type, register an explicit instance in `Converters` instead:

```csharp
options.Converters.Add(
    new EnumerationJsonConverter<OrderStatus, int>(EnumerationJsonSerializationFormat.DisplayName));
```

Converters registered earlier in the `Converters` list take precedence over later ones, so add specific converters **before** the factory.

## Native AOT and trimming

`EnumerationJsonConverterFactory` calls `MakeGenericType` to discover the value type, so it is not fully AOT friendly. For Native AOT apps:

1. Register `EnumerationJsonConverter<TEnumeration, TValue>` explicitly for each enumeration, and
2. use a source-generated `JsonSerializerContext` for your DTOs:

```csharp
[JsonSourceGenerationOptions(
    Converters = new[] { typeof(EnumerationJsonConverter<OrderStatus, int>) })]
[JsonSerializable(typeof(OrderDto))]
internal partial class AppJsonContext : JsonSerializerContext { }
```

The concrete converter reads and writes primitive values without reflection, and the enumeration types are annotated with `DynamicallyAccessedMembers(PublicFields)` so the trimmer keeps their declared members.

## Usage notes and limitations

- **Dictionary keys.** The converter is a value converter and does not implement dictionary key conversion. Use `.Value` or `.DisplayName` as the key and map back with `FromValue` / `FromName`.
- **Write format is global per converter instance.** You cannot mix value and name output for the same enumeration type in one `JsonSerializerOptions`.
- **Unknown values fail loudly.** A payload with an undefined value throws on deserialization. To accept unknown values, bind to the raw type and use `FromValueOrDefault`.
- **Name collisions.** If two members share a display name differing only by case, the case-insensitive match returns the first declared one.
- **Related packages.** For model validation use `Xibalbalabs.Enumeration.AspNetCore`; for persistence use `Xibalbalabs.Enumeration.EntityFrameworkCore` or `Xibalbalabs.Enumeration.Dapper`.
