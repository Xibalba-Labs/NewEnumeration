# Migrating from a legacy enumeration package to Xibalbalabs.Enumeration

This guide explains how to move an existing codebase off the legacy enumeration NuGet package
and onto **Xibalbalabs.Enumeration** — a modern, actively maintained, drop-in replacement that supports
.NET Standard 2.0 through .NET 9+, Native AOT, `System.Text.Json`, EF Core, Dapper, and ASP.NET Core model
validation.

There are two supported migration paths:

- **Path A — Zero-effort swap.** Replace the package reference only. Your existing `Enumeration<...>`
  classes keep compiling and behaving exactly as before, via an `[Obsolete]` compatibility shim.
- **Path B — Full migration.** Additionally repoint your `using` directives at the new namespace and adopt the
  new APIs (safe parsing, case-insensitive lookups, JSON/EF Core/Dapper integrations, etc.).

You can do Path A today and Path B incrementally, file by file, whenever convenient — the two coexist without
conflict.

> **Note:** throughout this guide, `<LegacyNamespace>` stands for the namespace your code currently imports
> from the legacy package (the one in your existing `using` directives). The compatibility shim re-declares
> its types under that same namespace, which is why no source changes are needed in Path A.

---

## Table of contents

1. [Why migrate](#1-why-migrate)
2. [Path A: zero-effort package swap](#2-path-a-zero-effort-package-swap)
3. [Path B: full migration to the new namespace](#3-path-b-full-migration-to-the-new-namespace)
4. [API compatibility matrix](#4-api-compatibility-matrix)
5. [New capabilities available after migrating](#5-new-capabilities-available-after-migrating)
   - [5.1 Safe parsing & case-insensitive lookups](#51-safe-parsing--case-insensitive-lookups)
   - [5.2 Operators and explicit casts](#52-operators-and-explicit-casts)
   - [5.3 `System.Text.Json` support](#53-systemtextjson-support)
   - [5.4 ASP.NET Core model validation](#54-aspnet-core-model-validation)
   - [5.5 Entity Framework Core value converters](#55-entity-framework-core-value-converters)
   - [5.6 Dapper type handler](#56-dapper-type-handler)
6. [Target framework support per package](#6-target-framework-support-per-package)
7. [Known gotchas and edge cases](#7-known-gotchas-and-edge-cases)
8. [Step-by-step checklist](#8-step-by-step-checklist)
9. [FAQ](#9-faq)

---

## 1. Why migrate

The legacy package has been unmaintained for years: no .NET Standard 2.1/`.NET 5+` support statement,
no Native AOT compatibility, no `System.Text.Json` converters, no EF Core `ValueConverter`, no Dapper
`TypeHandler`, and no case-insensitive/try-pattern APIs that avoid `try`/`catch` in hot paths.

`Xibalbalabs.Enumeration` re-implements the exact same "smart enum" pattern — value + display name, `GetAll()`,
`FromValue()`, `Parse()` — on top of a modern, multi-targeted, nullable-annotated, trimming/AOT-friendly core,
and ships the integrations most real projects end up hand-rolling themselves (JSON, EF Core, Dapper, ASP.NET
Core validation).

---

## 2. Path A: zero-effort package swap

If you just want your existing code to keep compiling with no source changes:

1. Remove the reference to the legacy package.
2. Add a reference to `Xibalbalabs.Enumeration`.
3. Build. That's it — nothing else should need to change.

This works because the package ships a compatibility shim in the legacy package's original namespace (written `<LegacyNamespace>` below)
(`src/Xibalbalabs.Enumeration/Legacy/Enumeration.cs`) that mirrors **both** original generic shapes:

```csharp
namespace <LegacyNamespace>
{
    // The int-backed shorthand, e.g. `class OrderStatus : Enumeration<OrderStatus>`
    public abstract class Enumeration<TEnumeration> : Xibalbalabs.Enumeration.Core.Enumeration<TEnumeration>
        where TEnumeration : Enumeration<TEnumeration> { /* ... */ }

    // The fully generic form, e.g. `class CountryCode : Enumeration<CountryCode, string>`
    public abstract class Enumeration<TEnumeration, TValue> : Xibalbalabs.Enumeration.Core.Enumeration<TEnumeration, TValue>
        where TEnumeration : Enumeration<TEnumeration, TValue>
        where TValue : IComparable { /* ... */ }
}
```

Both classes are marked `[Obsolete("This legacy enumeration API is obsolete. Consider migrating namespaces to
Xibalbalabs.Enumeration.Core in the future.")]` — a **warning**, not an error, so your build keeps succeeding.
You'll see the warning at every declaration site (e.g. `class OrderStatus : Enumeration<OrderStatus>`), which
is a useful, low-pressure reminder to move to Path B whenever you get to it.

Everything your code already calls — `FromValue`, `FromInt32`, `TryFromInt32`, `Parse`, `TryParse`, `GetAll`,
`Equals`, `GetHashCode`, `CompareTo`, `ToString`, `==`/`!=` between two instances, `==`/`!=` against the raw
`int` value, and the `(string)`/`(int)` casts — is reachable through this shim exactly as it was in the
original package. See [§4](#4-api-compatibility-matrix) for the full member-by-member mapping and
[§7](#7-known-gotchas-and-edge-cases) for the one narrow scenario (non-`int`-backed enums doing direct
value comparisons) that behaves slightly differently by necessity.

---

## 3. Path B: full migration to the new namespace

When you're ready to drop the `[Obsolete]` warnings and get access to the new features, migrate a file at a
time:

**Before:**

```csharp
using <LegacyNamespace>;

public sealed class OrderStatus : Enumeration<OrderStatus>
{
    public static readonly OrderStatus Pending = new OrderStatus(1, "Pending");
    public static readonly OrderStatus Shipped = new OrderStatus(2, "Shipped");

    private OrderStatus(int value, string displayName) : base(value, displayName) { }
}
```

**After:**

```csharp
using Xibalbalabs.Enumeration.Core;

public sealed class OrderStatus : Enumeration<OrderStatus>
{
    public static readonly OrderStatus Pending = new OrderStatus(1, "Pending");
    public static readonly OrderStatus Shipped = new OrderStatus(2, "Shipped");

    private OrderStatus(int value, string displayName) : base(value, displayName) { }
}
```

The only change is the `using` directive — the class body, base type name, and constructor are untouched.
The same applies to a two-parameter, non-`int`-backed enumeration:

```csharp
// Before: using <LegacyNamespace>;
// After:
using Xibalbalabs.Enumeration.Core;

public sealed class CountryCode : Enumeration<CountryCode, string>
{
    public static readonly CountryCode US = new CountryCode("US", "United States");
    public static readonly CountryCode CA = new CountryCode("CA", "Canada");

    private CountryCode(string value, string displayName) : base(value, displayName) { }
}
```

Because both namespaces expose a type literally named `Enumeration<...>`, **do not** have both
`using <LegacyNamespace>;` and `using Xibalbalabs.Enumeration.Core;` in the same file — that reintroduces exactly
the kind of ambiguity discussed in [§7](#7-known-gotchas-and-edge-cases). Migrate one `using` per file, not
partially.

Once migrated, also consider:

- Marking your enumeration classes `sealed` (as in the examples above) — they're concrete, non-abstract
  classes and this codebase's own convention requires it. It also lets the JIT/AOT compiler devirtualize
  calls.
- Replacing any `try { x = Enum.FromValue(v); } catch { x = fallback; }` block with `FromValueOrDefault` —
  see [§5.1](#51-safe-parsing--case-insensitive-lookups).

---

## 4. API compatibility matrix

| Original member | Status after migration | Notes |
|---|---|---|
| `Enumeration<TEnumeration>` (int-backed shorthand) | ✅ Identical | Same constructor signature `(int value, string displayName)` |
| `Enumeration<TEnumeration, TValue>` (generic form) | ✅ Identical | Same constructor signature `(TValue value, string displayName)` |
| `Value` / `DisplayName` properties | ✅ Identical | |
| `GetAll()` | ✅ Identical, now O(1)-backed internally | See [§6](#6-target-framework-support-per-package) for the `FrozenDictionary` note |
| `FromValue(TValue)` | ✅ Identical | Throws `ArgumentException` when not found, same as before |
| `FromInt32(int)` | ✅ Identical | Only on the int-backed shorthand |
| `Parse(string displayName)` | ✅ Identical | Throws `ArgumentException` when not found |
| `TryParse(TValue, out TEnumeration)` | ✅ Identical | |
| `TryParse(string, out TEnumeration)` | ✅ Identical | |
| `TryFromInt32(int, out TEnumeration)` | ✅ Identical | Only on the int-backed shorthand |
| `Equals(object)` / `Equals(TEnumeration)` / `GetHashCode()` | ✅ Identical (value-based) | Now annotated `object?`/`TEnumeration?` for nullable reference types — no behavioral change |
| `CompareTo(TEnumeration)` | ✅ Identical | |
| `ToString()` | ✅ Identical (returns `DisplayName`) | Still `sealed` |
| `operator ==` / `operator !=` (instance vs. instance) | ✅ Identical | |
| `operator ==` / `operator !=` (instance vs. raw value) | ✅ Available for `int`-backed enumerations only | New in this library; see [§7](#7-known-gotchas-and-edge-cases) for why it's not offered for arbitrary `TValue` |
| `(TValue)` explicit cast | ✅ **New**, available for every `TValue` | Not in the original package |
| `(string)` explicit cast to `DisplayName` | ✅ **New**, available for `int`-backed enumerations only | See [§7](#7-known-gotchas-and-edge-cases) |

Nothing that compiled and worked against the legacy package needs to change in Path A. Path B is a pure
`using`-directive change plus optional adoption of new members — it never removes anything.

---

## 5. New capabilities available after migrating

These are additive — none of them existed in the legacy package. All are on the `Core`
(`Xibalbalabs.Enumeration.Core`) types unless noted otherwise, so they're only reachable once you've moved a
class to Path B (they are **not** exposed through the legacy compatibility shim's obsolete surface,
though the shim classes do inherit the same base and therefore do get them too — see the table above).

### 5.1 Safe parsing & case-insensitive lookups

No more wrapping `FromValue`/`Parse` in `try`/`catch` to implement a fallback:

```csharp
// Try-pattern, no exception thrown on a miss:
if (OrderStatus.TryFromValue(statusId, out var status))
{
    // use status
}

if (OrderStatus.TryFromName("shipped", out var byName, ignoreCase: true))
{
    // use byName
}

// Or go straight to a default value:
var status = OrderStatus.FromValueOrDefault(statusId, OrderStatus.Pending);
var byName = OrderStatus.FromNameOrDefault("Shipped", OrderStatus.Pending, ignoreCase: true);

// FromName / FromDisplayName throw like Parse did, but support ignoreCase:
var exact = OrderStatus.FromName("Shipped");                 // case-sensitive, throws if missing
var loose = OrderStatus.FromDisplayName("shipped", ignoreCase: true);
```

`FromName` and `FromDisplayName` are equivalent — `FromDisplayName` exists purely so the intent reads clearly
at call sites that specifically care about the display string rather than a generic "name".

### 5.2 Operators and explicit casts

```csharp
// Comparing to the underlying value directly (int-backed enumerations only):
if (order.Status == 2) { /* ... */ }

// Casting to the underlying value (any TValue):
int id = (int)OrderStatus.Shipped;
string code = (string)CountryCode.US; // "US" — the Value, not the DisplayName, for non-int TValue

// Casting to the display name (int-backed enumerations only):
string label = (string)OrderStatus.Shipped; // "Shipped"
```

Read [§7](#7-known-gotchas-and-edge-cases) before relying on the `(string)` cast on a non-`int`-backed
enumeration — it returns `Value`, not `DisplayName`, and that's intentional.

### 5.3 `System.Text.Json` support

Package: **`Xibalbalabs.Enumeration.Json`** · Namespace: `Xibalbalabs.Enumeration.Serialization.Json`

```csharp
var options = new JsonSerializerOptions
{
    Converters = { new EnumerationJsonConverter<OrderStatus, int>() },
    // or, to serialize/deserialize using DisplayName instead of Value:
    // Converters = { new EnumerationJsonConverter<OrderStatus, int>(EnumerationJsonSerializationFormat.DisplayName) },
};

var json = JsonSerializer.Serialize(OrderStatus.Shipped, options); // "2"
var status = JsonSerializer.Deserialize<OrderStatus>("2", options); // OrderStatus.Shipped
```

Reads are lenient: a JSON string is first matched against `DisplayName` (case-insensitively), then falls back
to deserializing it as `TValue` — so a payload can mix numeric and name-based representations without extra
configuration.

For auto-registering converters across every `Enumeration<,>`-derived type in your model, use the factory
instead of listing each converter by hand:

```csharp
options.Converters.Add(new EnumerationJsonConverterFactory());
```

> **Native AOT note:** `EnumerationJsonConverter<TEnumeration, TValue>` is reflection-free for the common
> primitive value types (`string`, `int`, `long`, `short`, `byte`, `double`, `float`, `decimal`, `bool`,
> `Guid`) and safe to use in trimmed/AOT-published apps. `EnumerationJsonConverterFactory` uses
> `Type.MakeGenericType` to discover the value type at runtime and is **not** AOT-safe — prefer registering
> `EnumerationJsonConverter<TEnumeration, TValue>` directly per type when publishing Native AOT.

### 5.4 ASP.NET Core model validation

Package: **`Xibalbalabs.Enumeration.AspNetCore`** · Namespace: `Xibalbalabs.Enumeration.AspNetCore`

```csharp
public sealed class UpdateOrderRequest
{
    [EnumerationDefined(typeof(OrderStatus))]
    public int StatusId { get; set; }
}
```

Model binding/validation now rejects any `StatusId` that doesn't match a defined `OrderStatus` member, with no
extra code in your controller/endpoint. By default a `string` value matching `DisplayName`
(case-insensitively) is also accepted; set `AllowName = false` on the attribute to require the underlying
value only.

### 5.5 Entity Framework Core value converters

Package: **`Xibalbalabs.Enumeration.EntityFrameworkCore`** · Namespace: `Xibalbalabs.Enumeration.EntityFrameworkCore`
(targets `net8.0`/`net9.0` — see [§6](#6-target-framework-support-per-package))

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .Property(o => o.Status)
        .HasConversion(new EnumerationIdConverter<OrderStatus, int>()); // stores the int Value

    modelBuilder.Entity<Order>()
        .Property(o => o.Country)
        .HasConversion(new EnumerationNameConverter<CountryCode>()); // stores the DisplayName string
}
```

`EnumerationNameConverter<TEnumeration>` works for **any** `TValue`, since it always converts to/from
`DisplayName` regardless of the enumeration's underlying value type.

### 5.6 Dapper type handler

Package: **`Xibalbalabs.Enumeration.Dapper`** · Namespace: `Xibalbalabs.Enumeration.Dapper`

```csharp
SqlMapper.AddTypeHandler(new EnumerationTypeHandler<OrderStatus, int>());
```

Register once at startup, then query as usual — Dapper will materialize `OrderStatus` columns directly from
their underlying `int` value, and `DBNull` maps to `null`.

---

## 6. Target framework support per package

| Package | Target frameworks | Notes |
|---|---|---|
| `Xibalbalabs.Enumeration` | `netstandard2.0`, `net8.0`, `net9.0` | On `net8.0`+, internal lookups use `FrozenDictionary` for O(1) reads with no further allocation after first access; `netstandard2.0` uses a plain `Dictionary` with identical semantics |
| `Xibalbalabs.Enumeration.Json` | `netstandard2.0`, `net8.0`, `net9.0` | Pulls in the `System.Text.Json` NuGet package only on `netstandard2.0`; on `net8.0`/`net9.0` it uses the in-box implementation |
| `Xibalbalabs.Enumeration.AspNetCore` | `netstandard2.0`, `net8.0`, `net9.0` | Only depends on `System.ComponentModel.DataAnnotations`, not the ASP.NET Core shared framework — usable from any project type |
| `Xibalbalabs.Enumeration.EntityFrameworkCore` | `net8.0`, `net9.0` | EF Core 8+ itself no longer supports `netstandard2.0`, so this package follows suit |
| `Xibalbalabs.Enumeration.Dapper` | `netstandard2.0`, `net8.0`, `net9.0` | |

If your project targets classic .NET Framework (via `netstandard2.0`) and uses EF Core, you'll need to stay
on an older EF Core major version compatible with your runtime — `Xibalbalabs.Enumeration.EntityFrameworkCore`
itself requires `net8.0`+ regardless.

---

## 7. Known gotchas and edge cases

### 7.1 Direct comparison to the underlying value is `int`-only

`operator ==`/`!=` against the raw `TValue`, and the `(string)` cast to `DisplayName`, are only defined on
`Enumeration<TEnumeration>` — the `int`-backed shorthand — **not** on the fully generic
`Enumeration<TEnumeration, TValue>` base. This is intentional, not an oversight: `TValue` can be a reference
type (most commonly `string`). If those operators were defined generically, then for any `string`-backed
enumeration:

```csharp
CountryCode? maybeNull = null;
if (maybeNull == null) { /* ... */ }   // would be a compile-time ambiguity error
```

would fail to compile, because the `null` literal would match *both* `operator ==(Enumeration<T,TValue>?,
Enumeration<T,TValue>?)` *and* a hypothetical `operator ==(Enumeration<T,TValue>?, TValue)` (since `string`
accepts `null`). Similarly, a second `explicit operator string(...)` for `DisplayName` would collide with the
cast to the underlying value whenever `TValue` is itself `string` — you cannot declare two conversion
operators with the same source/target type pair in C#.

**Practical effect:** for a `string`-backed (or otherwise reference-type-backed) enumeration, `(string)value`
returns the underlying `Value`, and there's no operator overload for `someEnum == "US"` — use
`someEnum.Value == "US"`, `someEnum == CountryCode.US`, or `someEnum.Equals(CountryCode.US)` instead. Ordinary
`== null` / `!= null` comparisons work exactly as expected in every case.

### 7.2 Don't mix `<LegacyNamespace>` and `Xibalbalabs.Enumeration.Core` usings in the same file

Both namespaces declare a type literally named `Enumeration<...>`. Having both `using` directives in one file
makes any unqualified `Enumeration<...>` reference ambiguous. Migrate a whole file's `using` directive at
once (Path A → Path B), never partially within a file.

### 7.3 The `[Obsolete]` warning is intentional and safe to ignore short-term

It's a compiler **warning**, not an error, and won't fail your build (including under most CI configurations
that don't treat warnings as errors). Suppress it locally with `#pragma warning disable CS0618` around a
specific file if you need a clean warnings list before you're ready to migrate that file, but treat that as
a temporary measure — the underlying goal is still to move to Path B.

### 7.4 Error message text changed slightly

`FromValue`/`Parse`'s thrown `ArgumentException.Message` wording is slightly different from the original
package's exact phrasing (still along the lines of `"'{value}' is not a valid value in {type}"`). If any test
asserts on the literal message string rather than just the exception type, update that assertion.

---

## 8. Step-by-step checklist

- [ ] Remove the legacy package reference from every project that has it.
- [ ] Add a reference to `Xibalbalabs.Enumeration` in those same projects.
- [ ] Build the solution. Confirm it succeeds (expect `[Obsolete]` warnings, not errors) — this is Path A,
      done.
- [ ] For each file with a warning, when convenient: replace `using <LegacyNamespace>;` with
      `using Xibalbalabs.Enumeration.Core;` and rebuild that project. This is Path B for that file.
- [ ] Replace any `try { FromValue(...) } catch { ... }` fallback pattern with `TryFromValue`/
      `FromValueOrDefault` (and the `*Name*` equivalents where you parse display names).
- [ ] If you serialize these enumerations to JSON by hand today (e.g. a custom converter, or serializing
      `.Value`/`.DisplayName` manually), add `Xibalbalabs.Enumeration.Json` and register
      `EnumerationJsonConverter<,>` instead.
- [ ] If you have EF Core entities storing the raw `int`/`string` and mapping manually to the enumeration in
      application code, add `Xibalbalabs.Enumeration.EntityFrameworkCore` and configure a `ValueConverter`
      instead.
- [ ] If you use Dapper and materialize these columns manually today, add `Xibalbalabs.Enumeration.Dapper`
      and register the `TypeHandler` once at startup.
- [ ] If you have hand-rolled `[Custom]ValidationAttribute`s checking that an incoming int/string is a
      defined enumeration member, replace them with `[EnumerationDefined(typeof(...))]` from
      `Xibalbalabs.Enumeration.AspNetCore`.
- [ ] Mark migrated enumeration classes `sealed`, per this codebase's convention.

---

## 9. FAQ

**Do I have to migrate everything at once?**
No. Path A alone is a complete, permanent solution if you just want off the unmaintained package. Path B can
happen file by file, indefinitely, with both namespaces coexisting in the solution (just not the same file).

**Will my persisted data (database columns, JSON payloads) change format?**
No. `Value`/`DisplayName` semantics, `FromValue`/`Parse` matching rules, and `ToString()` output are all
unchanged. Existing serialized/stored data is fully compatible.

**Does this affect binary compatibility for already-compiled consumers?**
The legacy shim's public members are IL-equivalent to the original package (nullable annotations are
metadata-only and don't change method signatures at the CLR level), so already-compiled assemblies referencing
the old legacy assembly are unaffected by this migration; you only need to rebuild the projects
whose source code you actually change.

**What if I need `==` against the raw value for a non-`int` enumeration?**
Use `.Value` explicitly: `someEnum.Value == "US"`. This is one line longer than the sugar available for
`int`-backed enumerations, but avoids the compile-breaking ambiguity described in
[§7.1](#71-direct-comparison-to-the-underlying-value-is-int-only).

**Where do I ask questions or report a compatibility gap?**
Open an issue against this repository describing the exact legacy-package usage that doesn't
compile or behave identically after the swap — true drop-in compatibility for both generic shapes is a
explicit design goal, so any gap is treated as a bug.
