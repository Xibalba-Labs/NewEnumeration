using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xibalbalabs.Enumeration.Core;

namespace Xibalbalabs.Enumeration.EntityFrameworkCore;

/// <summary>
/// An Entity Framework Core <see cref="ValueConverter{TModel, TProvider}"/> that maps an
/// <see cref="Enumeration{TEnumeration, TValue}"/> to/from its underlying <typeparamref name="TValue"/> for storage.
/// </summary>
/// <typeparam name="TEnumeration">The concrete enumeration type.</typeparam>
/// <typeparam name="TValue">The type of the enumeration's underlying value, and the database column type.</typeparam>
public sealed class EnumerationIdConverter<TEnumeration, TValue> : ValueConverter<TEnumeration, TValue>
    where TEnumeration : Enumeration<TEnumeration, TValue>
    where TValue : IComparable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationIdConverter{TEnumeration, TValue}"/> class.
    /// </summary>
    public EnumerationIdConverter()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationIdConverter{TEnumeration, TValue}"/> class.
    /// </summary>
    /// <param name="mappingHints">Hints used by EF Core to determine the database column's facets.</param>
    public EnumerationIdConverter(ConverterMappingHints? mappingHints)
        : base(
            enumeration => enumeration.Value,
            value => Enumeration<TEnumeration, TValue>.FromValue(value),
            mappingHints)
    {
    }
}
