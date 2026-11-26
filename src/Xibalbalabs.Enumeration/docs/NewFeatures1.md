Act as a Lead C#/.NET Software Architect and NuGet Library Author.

We are enhancing our custom open-source Enumeration library (a modern, drop-in replacement for Headspring.Enumeration built on .NET Standard 2.0). The core replica of Headspring is already complete. Now, implement all the following value-added features to turn this library into a modern, production-ready, zero-allocation ecosystem for modern .NET (.NET Standard 2.0 through .NET 9+).

Ensure that all code strictly follows modern C# standards:
- Use file-scoped namespaces (`namespace MyEnumLibrary.Core;`).
- Concrete classes that are not abstract must be declared `sealed`.
- Helper classes exposed behind interfaces should be declared `internal` where appropriate.

Implement the following features divided by areas:

---

### FEATURE SET 1: Safe Parsing & Case-Insensitive Lookups (Core Library)
Add safe lookup methods to the base `Enumeration<TEnumeration, TValue>` class to eliminate the need for try/catch blocks:

1. **`TryFromValue` & `TryFromName`:**
   * Implement `public static bool TryFromValue(TValue value, out TEnumeration? result)`
   * Implement `public static bool TryFromName(string name, out TEnumeration? result, bool ignoreCase = false)`
2. **Case-Insensitive Lookups & Fallbacks:**
   * Add an optional `bool ignoreCase = false` parameter to `FromDisplayName` / `FromName`.
   * Implement `public static TEnumeration FromValueOrDefault(TValue value, TEnumeration defaultValue)`
   * Implement `public static TEnumeration FromNameOrDefault(string name, TEnumeration defaultValue, bool ignoreCase = false)`

---

### FEATURE SET 2: Modern C# Operators & Performance Optimizations
1. **Explicit/Implicit Conversions & Operators:**
   * Overload `==` and `!=` operators to allow comparing two enumerations or an enumeration directly with its underlying `TValue`.
   * Add explicit casting operators `(TValue)` and `(string)`.
2. **Fast static O(1) Indexing:**
   * Optimize internal dictionary lookups for `GetAll()` using static read-only dictionaries initialized once per generic type instantiation (`FrozenDictionary` or `ImmutableDictionary` for modern targets via conditional compilation `#if NET8_0_OR_GREATER`).

---

### FEATURE SET 3: Native `System.Text.Json` Support & Native AOT Compatibility
Create built-in `System.Text.Json` converters (or a sub-namespace `MyEnumLibrary.Serialization.Json`):

1. **`EnumerationJsonConverter<TEnumeration, TValue>`:**
   * Implement a `JsonConverter<TEnumeration>` that serializes the enumeration as its `TValue` (or optionally as its `Name`/`DisplayName` based on a configuration parameter).
   * Ensure full compatibility with **Native AOT (Ahead-Of-Time compilation)** in .NET 8/9 using reflection-free or source-generator compatible patterns.
2. **`EnumerationJsonConverterFactory`:**
   * Implement a `JsonConverterFactory` that automatically creates the appropriate converter for any class inheriting from `Enumeration<,>`.

---

### FEATURE SET 4: ASP.NET Core Validation Attribute
Create a Model Validation Attribute for ASP.NET Core DTOs:

* Path/Class: `[EnumerationDefinedAttribute]` (inheriting from `ValidationAttribute`).
* Usage: Can be placed on DTO properties (e.g., `public int StatusId { get; set; }`) to automatically validate that the incoming integer/string value exists within the specified `Enumeration` type during model binding.

---

### FEATURE SET 5: Sub-Packages / Extensions for ORM & Ecosystem Integrations
Create distinct extension classes or modules for popular data frameworks:

1. **Entity Framework Core Value Converters (`MyEnumLibrary.EntityFrameworkCore`):**
   * Create `EnumerationIdConverter<TEnumeration, TValue>` inheriting from `ValueConverter<TEnumeration, TValue>`.
   * Create `EnumerationNameConverter<TEnumeration>` inheriting from `ValueConverter<TEnumeration, string>`.
2. **Dapper Type Handlers (`MyEnumLibrary.Dapper`):**
   * Create `EnumerationTypeHandler<TEnumeration, TValue>` inheriting from Dapper's `SqlMapper.TypeHandler<TEnumeration>`.

---

### DELIVERABLES EXPECTED
1. Complete, clean, well-documented C# source code files for all the feature sets above.
2. XML documentation comments (`/// <summary>`) on all new public APIs.
3. Unit tests using xUnit verifying every new feature (Safe parsing, JSON serialization/deserialization, EF Core conversion, and Validation Attributes).