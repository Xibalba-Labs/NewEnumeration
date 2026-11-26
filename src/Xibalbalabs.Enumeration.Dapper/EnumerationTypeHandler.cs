using System;
using System.Data;
using System.Globalization;
using Dapper;
using Xibalbalabs.Enumeration.Core;

namespace Xibalbalabs.Enumeration.Dapper;

/// <summary>
/// A Dapper <see cref="SqlMapper.TypeHandler{T}"/> that maps an <see cref="Enumeration{TEnumeration, TValue}"/> to/from
/// its underlying <typeparamref name="TValue"/> column.
/// </summary>
/// <typeparam name="TEnumeration">The concrete enumeration type.</typeparam>
/// <typeparam name="TValue">The type of the enumeration's underlying value, and the database column type.</typeparam>
/// <example>
/// <code>
/// SqlMapper.AddTypeHandler(new EnumerationTypeHandler&lt;OrderStatus, int&gt;());
/// </code>
/// </example>
public sealed class EnumerationTypeHandler<TEnumeration, TValue> : SqlMapper.TypeHandler<TEnumeration>
    where TEnumeration : Enumeration<TEnumeration, TValue>
    where TValue : IComparable
{
    /// <inheritdoc />
    public override TEnumeration? Parse(object value)
    {
        if (value is null || value is DBNull)
        {
            return null;
        }

        var converted = value is TValue typedValue
            ? typedValue
            : (TValue)Convert.ChangeType(value, typeof(TValue), CultureInfo.InvariantCulture);

        return Enumeration<TEnumeration, TValue>.FromValue(converted);
    }

    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, TEnumeration? value)
    {
        parameter.Value = value is null ? (object)DBNull.Value : value.Value!;
    }
}
