# Xibalbalabs.Enumeration.Dapper

A Dapper `SqlMapper.TypeHandler<T>` for [Xibalbalabs.Enumeration](https://www.nuget.org/packages/Xibalbalabs.Enumeration) smart enums. Use enumerations directly in query parameters and in the objects Dapper materializes.

## Installation

```powershell
dotnet add package Xibalbalabs.Enumeration.Dapper
```

Targets `netstandard2.0`, `net8.0` and `net9.0`.

## Usage

Register one handler per enumeration type at startup (registration is global):

```csharp
using Dapper;
using Xibalbalabs.Enumeration.Dapper;

SqlMapper.AddTypeHandler(new EnumerationTypeHandler<OrderStatus, int>());
SqlMapper.AddTypeHandler(new EnumerationTypeHandler<CountryCode, string>());
```

Then use the types like any other:

```csharp
var orders = await connection.QueryAsync<Order>(
    "SELECT Id, Status FROM Orders WHERE Status = @Status",
    new { Status = OrderStatus.Shipped });
```

## Behavior

| Direction | Input | Result |
|---|---|---|
| Read | Value of the underlying type | Matching enumeration instance |
| Read | Another convertible numeric type (e.g. `long`) | Converted, then matched |
| Read | `NULL` | `null` |
| Read | Undefined value | `ArgumentException` |
| Write | Enumeration instance | Its `Value` |
| Write | `null` | `DBNull.Value` |

## Notes

- The handler stores the underlying value. To store display names, write a custom type handler.
- For `IN` clauses, pass the underlying values: `statuses.Select(s => s.Value)`.
- Declare properties nullable when the column can be `NULL`.

## License

MIT
