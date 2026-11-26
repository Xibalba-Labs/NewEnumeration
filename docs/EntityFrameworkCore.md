# Xibalbalabs.Enumeration.EntityFrameworkCore

Entity Framework Core value converters for `Xibalbalabs.Enumeration` smart enums. They let you use an enumeration directly as an entity property and choose whether the database stores its **underlying value** or its **display name**.

## Installation

```powershell
dotnet add package Xibalbalabs.Enumeration.EntityFrameworkCore
```

This also brings in the core package, `Xibalbalabs.Enumeration`, and `Microsoft.EntityFrameworkCore` (9.0.x).

| Target framework | Notes |
|---|---|
| `net8.0`, `net9.0` | `netstandard2.0` is not supported by this package |

## The two converters

| Converter | Stored in the database | Typical column |
|---|---|---|
| `EnumerationIdConverter<TEnumeration, TValue>` | The enumeration's `Value` | `int`, `smallint`, `varchar` (the `TValue` type) |
| `EnumerationNameConverter<TEnumeration>` | The enumeration's `DisplayName` | `varchar` / `nvarchar` |

Both are `ValueConverter` subclasses, so they work with the standard EF Core configuration APIs.

## Quick start

Given an enumeration:

```csharp
using Xibalbalabs.Enumeration.Core;

public sealed class OrderStatus : Enumeration<OrderStatus>
{
    public static readonly OrderStatus Pending = new(1, "Pending");
    public static readonly OrderStatus Shipped = new(2, "Shipped");
    public static readonly OrderStatus Cancelled = new(3, "Cancelled");

    private OrderStatus(int value, string displayName) : base(value, displayName) { }
}

public sealed class Order
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
}
```

### Option 1: configure a single property

```csharp
using Xibalbalabs.Enumeration.EntityFrameworkCore;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .Property(o => o.Status)
        .HasConversion(new EnumerationIdConverter<OrderStatus, int>());
}
```

Generic overload, equivalent:

```csharp
modelBuilder.Entity<Order>()
    .Property(o => o.Status)
    .HasConversion<EnumerationIdConverter<OrderStatus, int>>();
```

### Option 2: configure every property of a type (recommended)

Use model configuration conventions so that each `OrderStatus` property in the model is mapped the same way:

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder builder)
{
    builder.Properties<OrderStatus>()
        .HaveConversion<EnumerationIdConverter<OrderStatus, int>>();
}
```

Repeat the line for each enumeration type in your model.

## Storing the display name

Use `EnumerationNameConverter<TEnumeration>` when you want human-readable values in the table (useful for reporting or for databases queried by other tools):

```csharp
modelBuilder.Entity<Order>()
    .Property(o => o.Status)
    .HasConversion(new EnumerationNameConverter<OrderStatus>())
    .HasMaxLength(50);
```

It works for any enumeration, regardless of the underlying value type, including string-backed ones:

```csharp
public sealed class CountryCode : Enumeration<CountryCode, string>
{
    public static readonly CountryCode US = new("US", "United States");
    public static readonly CountryCode CA = new("CA", "Canada");

    private CountryCode(string value, string displayName) : base(value, displayName) { }
}

// Stores "Canada" for CountryCode.CA
builder.Properties<CountryCode>()
    .HaveConversion<EnumerationNameConverter<CountryCode>>();
```

For a string-backed enumeration, `EnumerationIdConverter<CountryCode, string>` stores `"CA"` instead.

## Choosing between value and name

| | Value (`EnumerationIdConverter`) | Name (`EnumerationNameConverter`) |
|---|---|---|
| Storage size | Small (an `int` is typical) | Larger (text) |
| Index and join speed | Faster | Slower |
| Readable in raw SQL | No (needs a lookup) | Yes |
| Survives renaming a member's display text | Yes | **No**, existing rows stop matching |
| Survives changing a member's value | **No** | Yes |

As a rule, prefer the value converter for new schemas and treat declared values as permanent. Use the name converter only when you accept display names as part of the database contract.

## Seed data and lookup tables

Enumerations are not entities, so EF will not create a lookup table for them by default. If you want a reference table with a foreign key, model it as an entity and seed it from `GetAll()`:

```csharp
public sealed class OrderStatusRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

modelBuilder.Entity<OrderStatusRow>().HasData(
    OrderStatus.GetAll().Select(s => new OrderStatusRow { Id = s.Value, Name = s.DisplayName }));
```

Keep the `Order.Status` property converted with `EnumerationIdConverter` and add a relationship to `OrderStatusRow` through a shadow or explicit foreign key as needed.

## Querying

Because the converter is applied by EF Core, comparisons against enumeration instances are translated to SQL against the stored value:

```csharp
var shipped = await db.Orders
    .Where(o => o.Status == OrderStatus.Shipped)
    .ToListAsync();
```

Be careful with operators that are not translatable. Calls such as `o.Status.Value > 1` or ordering by `o.Status` are evaluated against the converted column only when the converter preserves the ordering, which is true for `EnumerationIdConverter` over numeric values and not guaranteed for `EnumerationNameConverter` (it orders alphabetically). When in doubt, check the generated SQL with `ToQueryString()`.

## Nulls

EF Core does not run value converters for `null`. Declare the property as nullable and map the column as nullable to store a missing status:

```csharp
public OrderStatus? Status { get; set; }
```

## Error behavior

| Situation | Result |
|---|---|
| Database contains a value with no matching member (`EnumerationIdConverter`) | `ArgumentException` while materializing the entity |
| Database contains a name with no match (`EnumerationNameConverter`) | An exception from the underlying `FromName` lookup |
| Name casing differs from the declared display name | Not matched; the name converter is case-sensitive |

If your data may contain stale values, clean them up in a migration before introducing the converter.

## Usage notes and limitations

- **Column facets.** Both converters accept optional `ConverterMappingHints` in their constructors when you need to influence the provider's default column size or precision. Otherwise use `HasMaxLength` and friends on the property.
- **Reflection in the name converter.** `EnumerationNameConverter<TEnumeration>` uses reflection (cached per type) to support any `TValue`. `EnumerationIdConverter<,>` is fully generic and has no reflection.
- **Value comparer.** Enumeration instances are immutable singletons compared by `Value`, so the default EF Core change tracking comparison is sufficient.
- **Related packages.** For raw SQL with Dapper use `Xibalbalabs.Enumeration.Dapper`; for JSON use `Xibalbalabs.Enumeration.Json`.
