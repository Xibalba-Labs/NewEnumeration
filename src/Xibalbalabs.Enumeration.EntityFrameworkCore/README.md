# Xibalbalabs.Enumeration.EntityFrameworkCore

Entity Framework Core value converters for [Xibalbalabs.Enumeration](https://www.nuget.org/packages/Xibalbalabs.Enumeration) smart enums. Use an enumeration directly as an entity property and choose whether the database stores its value or its display name.

## Installation

```powershell
dotnet add package Xibalbalabs.Enumeration.EntityFrameworkCore
```

Targets `net8.0` and `net9.0`.

## Converters

| Converter | Stored in the database |
|---|---|
| `EnumerationIdConverter<TEnumeration, TValue>` | The enumeration's `Value` |
| `EnumerationNameConverter<TEnumeration>` | The enumeration's `DisplayName` (as `string`) |

## Usage

Map every property of a type through model conventions (recommended):

```csharp
using Xibalbalabs.Enumeration.EntityFrameworkCore;

protected override void ConfigureConventions(ModelConfigurationBuilder builder)
{
    builder.Properties<OrderStatus>()
        .HaveConversion<EnumerationIdConverter<OrderStatus, int>>();
}
```

Or configure a single property:

```csharp
modelBuilder.Entity<Order>()
    .Property(o => o.Status)
    .HasConversion(new EnumerationNameConverter<OrderStatus>())
    .HasMaxLength(50);
```

Queries translate normally:

```csharp
var shipped = await db.Orders.Where(o => o.Status == OrderStatus.Shipped).ToListAsync();
```

## Notes

- Prefer the value converter for new schemas; renaming a display name breaks rows stored by name.
- A stored value or name with no matching member throws when the entity is materialized.
- The name converter is case-sensitive.
- Declare the property nullable to store `NULL`.

## License

MIT
