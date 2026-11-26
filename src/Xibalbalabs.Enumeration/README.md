# Xibalbalabs.Enumeration

A lightweight, zero-dependency, high-performance **smart enum** library for .NET. It is a modern, maintained, drop-in replacement for legacy strongly-typed enumeration packages, with the same API surface plus faster lookups, nullable annotations and Native AOT support.

- Targets `netstandard2.0`, `net8.0` and `net9.0`
- No external dependencies
- O(1) lookups by value and by name (`FrozenDictionary` on .NET 8+)
- Trimming annotations and Native AOT compatibility
- Backward-compatible API through a legacy compatibility layer

## Installation

```powershell
dotnet add package Xibalbalabs.Enumeration
```

## Usage

```csharp
using Xibalbalabs.Enumeration.Core;

public sealed class OrderStatus : Enumeration<OrderStatus>
{
    public static readonly OrderStatus Pending = new(1, "Pending");
    public static readonly OrderStatus Shipped = new(2, "Shipped");

    private OrderStatus(int value, string displayName) : base(value, displayName) { }
}

var status = OrderStatus.FromValue(2);                    // Shipped
var byName = OrderStatus.FromName("shipped", ignoreCase: true);
if (OrderStatus.TryFromValue(99, out var missing)) { }    // false
var all = OrderStatus.GetAll();                           // Pending, Shipped
```

Use `Enumeration<TEnumeration, TValue>` for other underlying types, for example `string`:

```csharp
public sealed class CountryCode : Enumeration<CountryCode, string>
{
    public static readonly CountryCode US = new("US", "United States");

    private CountryCode(string value, string displayName) : base(value, displayName) { }
}
```

## API overview

| Category | Members |
|---|---|
| Properties | `Value`, `DisplayName` |
| Catalog | `GetAll()` |
| By value | `FromValue`, `FromValueOrDefault`, `TryFromValue` (`FromInt32`, `TryFromInt32` on the `int` variant) |
| By name | `FromName`, `FromDisplayName`, `FromNameOrDefault`, `TryFromName` (optional `ignoreCase`) |
| Compatibility | `Parse`, `TryParse` |
| Equality and order | `Equals`, `GetHashCode`, `CompareTo`, `==`, `!=` |

## Migrating from a legacy enumeration package

Replace the old package reference with `Xibalbalabs.Enumeration`. A compatibility layer exposes the original namespace and types, marked `[Obsolete]`, so existing code compiles unchanged. Move to the `Xibalbalabs.Enumeration.Core` namespace at your own pace.

## Companion packages

| Package | Purpose |
|---|---|
| `Xibalbalabs.Enumeration.Json` | `System.Text.Json` converters |
| `Xibalbalabs.Enumeration.EntityFrameworkCore` | EF Core value converters |
| `Xibalbalabs.Enumeration.Dapper` | Dapper type handler |
| `Xibalbalabs.Enumeration.AspNetCore` | Model validation attribute |

## License

MIT
