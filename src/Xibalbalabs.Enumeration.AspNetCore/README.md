# Xibalbalabs.Enumeration.AspNetCore

Model validation for [Xibalbalabs.Enumeration](https://www.nuget.org/packages/Xibalbalabs.Enumeration) smart enums. Provides `[EnumerationDefined]`, a `System.ComponentModel.DataAnnotations` attribute that checks a DTO value matches a declared enumeration member.

It has no dependency on ASP.NET Core itself, so it works wherever DataAnnotations validation runs: MVC, Razor Pages, Blazor forms or manual validation.

## Installation

```powershell
dotnet add package Xibalbalabs.Enumeration.AspNetCore
```

Targets `netstandard2.0`, `net8.0` and `net9.0`.

## Usage

```csharp
using Xibalbalabs.Enumeration.AspNetCore;

public sealed class UpdateOrderRequest
{
    [EnumerationDefined(typeof(OrderStatus))]
    public int StatusId { get; set; }
}
```

An invalid payload such as `{ "statusId": 999 }` fails validation (a `400` response with `[ApiController]`).

## Options

| Property | Default | Description |
|---|---|---|
| `AllowName` | `true` | A `string` value may also match a member's `DisplayName`, ignoring case. |

```csharp
[EnumerationDefined(typeof(OrderStatus), AllowName = false)]
public string? StatusCode { get; set; }
```

## Notes

- `null` is always valid; add `[Required]` when the value is mandatory.
- Works with string-backed enumerations; value matching is exact, name matching is case-insensitive.
- The default message is `The value '<member>' is not a valid <EnumerationType>.`; override it with `ErrorMessage`.
- The attribute only validates; it does not bind or convert request data.
- It uses reflection, so it is not trimming/AOT friendly.

## License

MIT
