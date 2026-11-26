# Xibalbalabs.Enumeration.Dapper

A Dapper `SqlMapper.TypeHandler<T>` that maps `Xibalbalabs.Enumeration` smart enums to and from the **underlying value** stored in a database column. Once registered, you can use enumerations directly in query parameters and in the objects Dapper materializes.

## Installation

```powershell
dotnet add package Xibalbalabs.Enumeration.Dapper
```

This also brings in the core package, `Xibalbalabs.Enumeration`, and `Dapper` (2.1.x).

| Target framework | Notes |
|---|---|
| `netstandard2.0`, `net8.0`, `net9.0` | No extra dependencies beyond Dapper |

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
```

Register the handler **once**, at application startup:

```csharp
using Dapper;
using Xibalbalabs.Enumeration.Dapper;

SqlMapper.AddTypeHandler(new EnumerationTypeHandler<OrderStatus, int>());
```

Then use the enumeration like any other type:

```csharp
public sealed class Order
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
}

// Reading: the INT column "Status" becomes an OrderStatus instance
var orders = await connection.QueryAsync<Order>(
    "SELECT Id, Status FROM Orders WHERE Status = @Status",
    new { Status = OrderStatus.Shipped });   // writing: sends the value 2

// Writing
await connection.ExecuteAsync(
    "UPDATE Orders SET Status = @Status WHERE Id = @Id",
    new { Id = 42, Status = OrderStatus.Cancelled });
```

## Generic parameters

`EnumerationTypeHandler<TEnumeration, TValue>` takes two type arguments:

| Parameter | Meaning |
|---|---|
| `TEnumeration` | The concrete enumeration class (`OrderStatus`) |
| `TValue` | The underlying value type, which is also the database column type (`int`, `string`, ...) |

For `Enumeration<T>` (the `int` shorthand), use `TValue = int`:

```csharp
SqlMapper.AddTypeHandler(new EnumerationTypeHandler<OrderStatus, int>());
```

For `Enumeration<T, string>`:

```csharp
SqlMapper.AddTypeHandler(new EnumerationTypeHandler<CountryCode, string>());
```

## Registration

`SqlMapper.AddTypeHandler` is **global and static**, and you need one registration per enumeration type. Register all of them in one place during startup:

```csharp
public static class DapperConfiguration
{
    public static void Register()
    {
        SqlMapper.AddTypeHandler(new EnumerationTypeHandler<OrderStatus, int>());
        SqlMapper.AddTypeHandler(new EnumerationTypeHandler<PaymentMethod, int>());
        SqlMapper.AddTypeHandler(new EnumerationTypeHandler<CountryCode, string>());
    }
}
```

In ASP.NET Core, call it from `Program.cs` before the app starts serving requests:

```csharp
DapperConfiguration.Register();
var app = builder.Build();
```

## Behavior

### Reading (`Parse`)

| Database value | Result |
|---|---|
| Value of type `TValue` (e.g. `2` for `int`) | The matching enumeration instance |
| A different type convertible to `TValue` (e.g. `long`, `short`, `decimal` for an `int` enumeration) | Converted with the invariant culture, then matched |
| `NULL` / `DBNull` | `null` |
| A value with no matching member | `ArgumentException` ("'999' is not a valid value in OrderStatus") |

The type conversion matters because some providers return a wider numeric type than the one declared on the enumeration (for example `bigint` columns, or SQLite, which returns `long` for every integer).

The returned instance is the same singleton declared on the enumeration, so reference comparisons such as `order.Status == OrderStatus.Shipped` work as expected.

### Writing (`SetValue`)

| Enumeration instance | Parameter value |
|---|---|
| A member | Its `Value` |
| `null` | `DBNull.Value` |

## Nullable enumerations

Declare the property as nullable when the column can contain `NULL`:

```csharp
public sealed class Shipment
{
    public int Id { get; set; }
    public OrderStatus? Status { get; set; }   // NULL column -> null
}
```

Without the nullable annotation, a `NULL` column still yields `null`; the compiler just will not warn you.

## Collections and `IN` clauses

Dapper expands enumerable parameters. Because the handler only applies to scalar parameters, map the values yourself for `IN` lists:

```csharp
var statuses = new[] { OrderStatus.Pending, OrderStatus.Shipped };

var orders = await connection.QueryAsync<Order>(
    "SELECT Id, Status FROM Orders WHERE Status IN @Statuses",
    new { Statuses = statuses.Select(s => s.Value) });
```

## Usage notes and limitations

- **Value-based only.** The handler persists `Value`. To store the `DisplayName` instead, write a small custom `SqlMapper.TypeHandler<T>` using `FromName` / `DisplayName`.
- **Unknown values throw.** If the table contains a value that is not declared on the enumeration (for example after removing a member), reading it throws `ArgumentException`. If you need tolerance, handle the column as the raw type and use `FromValueOrDefault` or `TryFromValue` in code.
- **One handler per type.** Registering the same type again replaces the previous handler for that type.
- **Dynamic and anonymous results.** Type handlers apply when Dapper maps to a typed property or parameter. With `dynamic` results you receive the raw column value.
- **Stored procedures and table-valued parameters** use the same handler for scalar parameters; table-valued parameters are not covered.
- **Related packages.** For EF Core use `Xibalbalabs.Enumeration.EntityFrameworkCore`; for JSON use `Xibalbalabs.Enumeration.Json`.
