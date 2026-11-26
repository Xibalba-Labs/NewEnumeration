using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xibalbalabs.Enumeration.Core;

namespace Xibalbalabs.Enumeration.EntityFrameworkCore;

/// <summary>
/// An Entity Framework Core <see cref="ValueConverter{TModel, TProvider}"/> that maps an
/// <see cref="Enumeration{TEnumeration, TValue}"/> to/from its <c>DisplayName</c> for storage, regardless of its
/// underlying value type.
/// </summary>
/// <typeparam name="TEnumeration">The concrete enumeration type, deriving from <c>Enumeration&lt;TEnumeration, TValue&gt;</c> for any <c>TValue</c>.</typeparam>
public sealed class EnumerationNameConverter<TEnumeration> : ValueConverter<TEnumeration, string>
    where TEnumeration : class
{
    private static readonly Func<TEnumeration, string> ToDisplayName = BuildToDisplayName();
    private static readonly Func<string, TEnumeration> FromDisplayName = BuildFromDisplayName();

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationNameConverter{TEnumeration}"/> class.
    /// </summary>
    public EnumerationNameConverter()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationNameConverter{TEnumeration}"/> class.
    /// </summary>
    /// <param name="mappingHints">Hints used by EF Core to determine the database column's facets.</param>
    public EnumerationNameConverter(ConverterMappingHints? mappingHints)
        : base(
            enumeration => ToDisplayName(enumeration),
            name => FromDisplayName(name),
            mappingHints)
    {
    }

    private static Func<TEnumeration, string> BuildToDisplayName()
    {
        var displayNameProperty = FindEnumerationBaseType(typeof(TEnumeration)).GetProperty("DisplayName")!;
        return enumeration => (string)displayNameProperty.GetValue(enumeration)!;
    }

    private static Func<string, TEnumeration> BuildFromDisplayName()
    {
        var baseType = FindEnumerationBaseType(typeof(TEnumeration));
        var fromNameMethod = baseType.GetMethod("FromName", new[] { typeof(string), typeof(bool) })!;
        return name => (TEnumeration)fromNameMethod.Invoke(null, new object[] { name, false })!;
    }

    private static Type FindEnumerationBaseType(Type enumerationType)
    {
        for (var type = enumerationType; type is not null && type != typeof(object); type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Enumeration<,>))
            {
                return type;
            }
        }

        throw new InvalidOperationException($"'{enumerationType}' does not derive from Enumeration<,>.");
    }
}
