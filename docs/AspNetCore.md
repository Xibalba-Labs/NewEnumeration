# Xibalbalabs.Enumeration.AspNetCore

Model validation for `Xibalbalabs.Enumeration` smart enums. The package provides a single `System.ComponentModel.DataAnnotations` attribute, `[EnumerationDefined]`, that checks that a DTO property, field or parameter holds a value that matches a declared member of an enumeration.

Despite the name, the package has **no dependency on ASP.NET Core itself**. It is a plain `ValidationAttribute`, so it works anywhere DataAnnotations validation runs: MVC controllers, Minimal APIs with a validation library, Razor Pages, Blazor forms, or a manual `Validator.TryValidateObject` call.

## Installation

```powershell
dotnet add package Xibalbalabs.Enumeration.AspNetCore
```

This also brings in the core package, `Xibalbalabs.Enumeration`.

| Target framework | Notes |
|---|---|
| `netstandard2.0` | Adds `System.ComponentModel.Annotations` |
| `net8.0`, `net9.0` | No extra dependencies |

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

Decorate the DTO:

```csharp
using Xibalbalabs.Enumeration.AspNetCore;

public sealed class UpdateOrderRequest
{
    [EnumerationDefined(typeof(OrderStatus))]
    public int StatusId { get; set; }
}
```

With `[ApiController]`, an invalid payload such as `{ "statusId": 999 }` is rejected automatically with a `400` validation problem response.

```csharp
[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    [HttpPut]
    public IActionResult Update(UpdateOrderRequest request)
    {
        var status = OrderStatus.FromValue(request.StatusId); // safe: already validated
        return Ok(status.DisplayName);
    }
}
```

## How validation works

For a non-null value the attribute:

1. Tries to convert the value to the enumeration's underlying value type (`int`, `string`, ...) using the invariant culture, and compares it to the `Value` of each member.
2. If the value is a `string` and `AllowName` is `true`, also compares it to each member's `DisplayName`, **ignoring case**.
3. Succeeds on the first match; otherwise returns a validation error.

| Property type | Input | Result (`OrderStatus`) |
|---|---|---|
| `int` | `2` | Valid |
| `int` | `999` | Invalid |
| `string` | `"Shipped"` | Valid (name match) |
| `string` | `"shipped"` | Valid (case-insensitive) |
| `string` | `"2"` | Valid (converted to `int`) |
| `string` | `"Unknown"` | Invalid |
| any | `null` | Valid (see below) |

### Options

| Property | Default | Description |
|---|---|---|
| `AllowName` | `true` | When `true`, a `string` value may also match a member's `DisplayName`. Set to `false` to accept only underlying values. |

```csharp
public sealed class FilterRequest
{
    // Accepts 1, 2, 3 or "Pending", "shipped", ...
    [EnumerationDefined(typeof(OrderStatus))]
    public string? Status { get; set; }

    // Accepts only values; names are rejected
    [EnumerationDefined(typeof(OrderStatus), AllowName = false)]
    public string? StatusCode { get; set; }
}
```

### String-backed enumerations

Works the same way for enumerations whose underlying value is not an `int`:

```csharp
public sealed class CountryCode : Enumeration<CountryCode, string>
{
    public static readonly CountryCode UnitedStates = new("US", "United States");
    public static readonly CountryCode Canada = new("CA", "Canada");

    private CountryCode(string value, string displayName) : base(value, displayName) { }
}

public sealed class AddressRequest
{
    [EnumerationDefined(typeof(CountryCode))]
    public string Country { get; set; } = "";
}
```

Here `"US"` matches by value and `"canada"` matches by name. Note that value comparison is exact (case-sensitive for strings); only the `DisplayName` match is case-insensitive.

## Null handling and required values

`null` is **always valid**, following the DataAnnotations convention that validation attributes other than `[Required]` do not reject missing values. Combine both attributes when the value is mandatory:

```csharp
public sealed class CreateOrderRequest
{
    [Required]
    [EnumerationDefined(typeof(OrderStatus))]
    public string? Status { get; set; }
}
```

For a non-nullable `int` property, a missing JSON field binds to `0`. If `0` is not a declared member, validation fails as expected; if it is, it is accepted.

## Error messages

The default message template is:

```
The value '{0}' is not a valid {1}.
```

`{0}` is the **member's display name** (for example `StatusId`) and `{1}` is the enumeration type name, so the rendered message is `The value 'StatusId' is not a valid OrderStatus.` The error is attached to the member name, so it appears under the right key in `ModelState` / `ValidationProblemDetails`.

To customize it, use the standard `ValidationAttribute` properties:

```csharp
[EnumerationDefined(typeof(OrderStatus), ErrorMessage = "Unknown order status.")]
public int StatusId { get; set; }
```

## Manual validation

```csharp
var request = new UpdateOrderRequest { StatusId = 999 };
var results = new List<ValidationResult>();

bool ok = Validator.TryValidateObject(
    request,
    new ValidationContext(request),
    results,
    validateAllProperties: true);
// ok == false; results[0].MemberNames contains "StatusId"
```

## Usage notes and limitations

- **Validation only.** The attribute does not bind or convert request data. It does not turn an `int` into an `OrderStatus`. Bind to the raw value, validate, then call `FromValue` / `FromName`. For JSON bodies that bind directly to the enumeration type, see `Xibalbalabs.Enumeration.Json`.
- **Constructor checks.** Passing a type that does not derive from `Enumeration<,>` throws `ArgumentException` when the attribute is instantiated, and `null` throws `ArgumentNullException`.
- **Reflection.** Member lookup uses reflection on each validation call. This is fine for request validation, but the attribute is not trimming/Native AOT friendly. Under AOT, validate with a custom check using `TryFromValue` / `TryFromName` instead.
- **Inherited members.** Only members discovered by `GetAll()` are considered (public static fields declared directly on the enumeration type).
- **Attribute scope.** Applies to properties, fields and parameters; not repeatable (`AllowMultiple = false`).
