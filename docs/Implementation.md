# Implementation Guide

This document describes, at a high level, how **Xibalbalabs.Enumeration** is built and how to adopt it as an improved replacement for the legacy strongly-typed enumeration ("smart enum") library that many .NET projects still depend on. Throughout the text, that library is referred to simply as **the legacy library** or **the original package**.

---

## 1. Design goals

| Goal | How it is achieved |
|---|---|
| **API compatibility** | Public type and member names of the original package are preserved (`Value`, `DisplayName`, `GetAll`, `FromValue`, `Parse`, `TryParse`, etc.). |
| **Frictionless migration** | A compatibility layer exposes the same types under the legacy namespace, inheriting from the new implementation. |
| **Performance** | O(1) lookups through dictionaries built once (`FrozenDictionary` on .NET 8+). |
| **Modern .NET support** | Multi-targets `netstandard2.0`, `net8.0` and `net9.0`; trimming annotations and Native AOT compatibility in the core. |
| **Zero dependencies in the core** | The main package references no NuGet packages; integrations live in satellite packages. |
| **First-class integrations** | `System.Text.Json`, EF Core, Dapper and ASP.NET Core validation. |

---

## 2. Solution architecture

```
src/
├── Xibalbalabs.Enumeration                  # Core (no dependencies)
│   ├── Core/Enumeration.cs                  # Actual implementation
│   └── Legacy/Enumeration.cs                # Compatibility layer (legacy namespace)
├── Xibalbalabs.Enumeration.Json             # System.Text.Json
├── Xibalbalabs.Enumeration.EntityFrameworkCore
├── Xibalbalabs.Enumeration.Dapper
├── Xibalbalabs.Enumeration.AspNetCore       # Validation (DataAnnotations)
└── Xibalbalabs.Enumeration.Tests            # xUnit
```

Each integration depends only on the core and on its own target framework, so consumers install only what they use.

| Package | Target frameworks | External dependency |
|---|---|---|
| `Xibalbalabs.Enumeration` | netstandard2.0, net8.0, net9.0 | None |
| `Xibalbalabs.Enumeration.Json` | netstandard2.0, net8.0, net9.0 | `System.Text.Json` (netstandard2.0 only) |
| `Xibalbalabs.Enumeration.EntityFrameworkCore` | net8.0, net9.0 | `Microsoft.EntityFrameworkCore` |
| `Xibalbalabs.Enumeration.Dapper` | netstandard2.0, net8.0, net9.0 | `Dapper` |
| `Xibalbalabs.Enumeration.AspNetCore` | netstandard2.0, net8.0, net9.0 | `System.ComponentModel.Annotations` (netstandard2.0 only) |

---

## 3. The core: `Xibalbalabs.Enumeration.Core`

### 3.1 Type hierarchy

```
Enumeration<TEnumeration, TValue>      ← generic base (TValue : IComparable)
        ▲
Enumeration<TEnumeration>              ← shorthand with TValue = int
```

- **`Enumeration<TEnumeration, TValue>`** holds all the logic: storage of `Value` and `DisplayName`, equality, comparison, member discovery and lookups.
- **`Enumeration<TEnumeration>`** is the shorthand for `int`-backed enumerations. It adds `FromInt32`/`TryFromInt32`, the `==`/`!=` operators against `int`, and the explicit conversion to `string`.

> **Design decision:** the operators against the underlying value and the conversion to `string` exist only on the `int` variant. If they were on the generic base, a reference-type `TValue` (for example `string`) would cause operator ambiguities even in trivial comparisons such as `x == null`.

### 3.2 Declaring an enumeration

```csharp
using Xibalbalabs.Enumeration.Core;

public sealed class OrderStatus : Enumeration<OrderStatus>
{
    public static readonly OrderStatus Pending = new(1, "Pending");
    public static readonly OrderStatus Shipped = new(2, "Shipped");

    private OrderStatus(int value, string displayName) : base(value, displayName) { }
}

// With a non-integer value:
public sealed class CountryCode : Enumeration<CountryCode, string>
{
    public static readonly CountryCode Mexico = new("MX", "Mexico");

    private CountryCode(string value, string displayName) : base(value, displayName) { }
}
```

### 3.3 Member discovery

`GetAll()` uses reflection **once** per type: it takes the `public static` fields declared directly on the type whose field type is assignable to `TEnumeration`. The result is stored in a static `Lazy<TEnumeration[]>` (thread-safe).

To stay trimming/AOT friendly, the `TEnumeration` generic parameter is annotated with `DynamicallyAccessedMembers(PublicFields)` (on `net5.0+`), telling the trimmer to preserve the public fields of every enumeration.

### 3.4 Lookup indexes

Three lazily built indexes are derived from `GetAll()`:

| Index | Key | Comparer |
|---|---|---|
| `_byValue` | `TValue` | `EqualityComparer<TValue>.Default` |
| `_byDisplayName` | `string` | `Ordinal` |
| `_byDisplayNameIgnoreCase` | `string` | `OrdinalIgnoreCase` |

On `net8.0+` they are `FrozenDictionary` instances, optimized for read-heavy access; on `netstandard2.0` a regular `Dictionary` is used. When duplicate keys exist, the first declared member wins and no exception is thrown (tolerant behavior).

### 3.5 Public API

| Category | Members |
|---|---|
| Properties | `Value`, `DisplayName` |
| Catalog | `GetAll()` |
| By value | `FromValue`, `FromValueOrDefault`, `TryFromValue`, `FromInt32`*, `TryFromInt32`* |
| By name | `FromName`, `FromDisplayName`, `FromNameOrDefault`, `TryFromName` (all with optional `ignoreCase`) |
| Compatibility | `Parse(string)`, `TryParse(TValue, out)`, `TryParse(string, out)` |
| Equality / ordering | `Equals`, `GetHashCode`, `CompareTo`, `==`, `!=` |
| Conversion | `explicit operator TValue`; `explicit operator string`* |
| Text | `ToString()` (sealed, returns `DisplayName`) |

\* Only on `Enumeration<TEnumeration>` (the `int` variant).

**Improvements over the original package**

- Name lookup with an `ignoreCase` option, without scanning the collection.
- `...OrDefault` variants to avoid exceptions in normal flows.
- `TryFromName` tolerates `null` and returns `false`.
- Nullable reference types enabled and complete XML documentation.
- Immutable, cached indexes instead of linear scans on every call.

---

## 4. Compatibility layer (`Legacy`)

The `Legacy/Enumeration.cs` file declares, **under the original package's namespace**, two abstract classes with the same generic signatures as the original:

```csharp
Enumeration<TEnumeration>             : Core.Enumeration<TEnumeration>
Enumeration<TEnumeration, TValue>     : Core.Enumeration<TEnumeration, TValue>
```

Both are marked `[Obsolete]` and add no logic: they only forward the constructor. This lets existing code compile **without changing a single line of domain code**: replacing the NuGet package reference is enough.

The `[Obsolete]` attribute produces a compile-time warning that encourages moving to the new namespace when convenient, without forcing a big-bang migration.

---

## 5. Integrations

### 5.1 System.Text.Json — `Xibalbalabs.Enumeration.Json`

- **`EnumerationJsonConverter<TEnumeration, TValue>`**: concrete converter, AOT compatible.
- **`EnumerationJsonConverterFactory`**: automatically detects any type deriving from `Enumeration<,>` and creates the right converter. It uses reflection (`MakeGenericType`), so it is **not fully AOT-friendly**; in AOT scenarios, register the concrete converters per type.
- **`EnumerationJsonSerializationFormat`**: `Value` (default) or `DisplayName`.

```csharp
var options = new JsonSerializerOptions();
options.Converters.Add(new EnumerationJsonConverterFactory());                                  // by value
// options.Converters.Add(new EnumerationJsonConverterFactory(EnumerationJsonSerializationFormat.DisplayName));
```

### 5.2 Entity Framework Core — `Xibalbalabs.Enumeration.EntityFrameworkCore`

- **`EnumerationIdConverter<TEnumeration, TValue>`**: persists the `Value`.
- **`EnumerationNameConverter<TEnumeration>`**: persists the `DisplayName` as a `string`.

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder builder)
{
    builder.Properties<OrderStatus>().HaveConversion<EnumerationIdConverter<OrderStatus, int>>();
}
```

### 5.3 Dapper — `Xibalbalabs.Enumeration.Dapper`

`EnumerationTypeHandler<TEnumeration, TValue>` reads the column (converting the type if the provider returns a different numeric type) and writes `DBNull` for null values.

```csharp
SqlMapper.AddTypeHandler(new EnumerationTypeHandler<OrderStatus, int>());
```

### 5.4 ASP.NET Core — `Xibalbalabs.Enumeration.AspNetCore`

`[EnumerationDefined(typeof(T))]` validates that a DTO holds a defined member, either by value or, with `AllowName = true` (the default), by case-insensitive `DisplayName`.

```csharp
public sealed class UpdateOrderRequest
{
    [EnumerationDefined(typeof(OrderStatus))]
    public int StatusId { get; set; }
}
```

---

## 6. Migration plan from the legacy library

### Phase 1 — Package swap (no code changes)

1. Remove the NuGet reference to the legacy library.
2. Add `Xibalbalabs.Enumeration`.
3. Build. Existing `using` directives and base classes keep resolving thanks to the `Legacy` layer.
4. Run the existing test suite.
5. Review the (expected) `[Obsolete]` warnings and the potential incompatibilities listed in section 7.

### Phase 2 — Adopt the integrations

Replace ad hoc converters and serializers with the satellite packages (JSON, EF Core, Dapper, validation).

### Phase 3 — Namespace migration (optional, incremental)

1. Replace `using <legacy namespace>;` with `using Xibalbalabs.Enumeration.Core;`.
2. Do it module by module or project by project; both namespaces can coexist.
3. Once no references remain, the `[Obsolete]` warnings disappear entirely.

### Phase 4 — Take advantage of the improvements

Use `FromNameOrDefault`, `ignoreCase` lookups, `EnumerationDefined`, etc., and retire in-house helper code.

---

## 7. Compatibility notes and caveats

| Topic | Detail |
|---|---|
| **Compatibility tests** | The test project includes a dedicated suite (`Legacy/`) with sample enumerations written against the legacy API to verify they keep working. |
| **Duplicate keys** | The indexes keep the first declared member; they do not throw. |
| **Operators** | `==`/`!=` against `int` and the conversion to `string` are available only on the `Enumeration<TEnumeration>` variant. |
| **Reflection** | `GetAll()` relies on public static fields declared directly on the type (`DeclaredOnly`). Members declared in an intermediate base class are not discovered. |
| **AOT / trimming** | Core is annotated; the JSON factory requires explicit converters under AOT; the ASP.NET Core validation attribute uses reflection. |
| **EF Core** | `net8.0` and `net9.0` only. |
| **Binary serialization** | Types keep `[Serializable]` for compatibility. |

---

## 8. Testing strategy

- Core tests (`Core/EnumerationTests`): lookups, equality, ordering, operators, null cases.
- Compatibility tests (`Legacy/`): sample enumerations written against the legacy API.
- Per-integration tests: JSON, EF Core, Dapper and ASP.NET Core.
- Shared fixtures in `TestFixtures/` (`Colour`, `CountryCode`).

Run with: `dotnet test` from the root of `src/`.

---

## 9. Packaging and publishing

- One NuGet package per project; the core is published as `Xibalbalabs.Enumeration`.
- XML documentation generation is enabled for IntelliSense.
- `IsAotCompatible` is enabled on the `net8.0` and `net9.0` targets of the core and the JSON package.
- Semantic versioning is recommended: changes to the compatibility layer should be treated as breaking.

---

## 10. Summary

1. Replace the legacy package with `Xibalbalabs.Enumeration`.
2. Build and run the tests; existing code should work unchanged.
3. Add the integration packages you need.
4. Progressively migrate to the `Xibalbalabs.Enumeration.Core` namespace.
5. Take advantage of the performance, nullability and API improvements.
