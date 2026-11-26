using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Xibalbalabs.Enumeration.Core;

namespace Xibalbalabs.Enumeration.AspNetCore;

/// <summary>
/// Validates that a DTO property, field, or parameter's value corresponds to a defined member of an
/// <see cref="Enumeration{TEnumeration, TValue}"/> type, by either its underlying value or (optionally) its display name.
/// </summary>
/// <example>
/// <code>
/// public sealed class UpdateOrderRequest
/// {
///     [EnumerationDefined(typeof(OrderStatus))]
///     public int StatusId { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class EnumerationDefinedAttribute : ValidationAttribute
{
    private readonly Type _enumerationType;
    private readonly Type _valueType;
    private readonly MethodInfo _getAllMethod;
    private readonly PropertyInfo _valueProperty;
    private readonly PropertyInfo _displayNameProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationDefinedAttribute"/> class.
    /// </summary>
    /// <param name="enumerationType">A type deriving from <see cref="Enumeration{TEnumeration, TValue}"/>.</param>
    public EnumerationDefinedAttribute(Type enumerationType)
        : base("The value '{0}' is not a valid {1}.")
    {
        if (enumerationType is null)
        {
            throw new ArgumentNullException(nameof(enumerationType));
        }

        var baseType = FindEnumerationBaseType(enumerationType)
            ?? throw new ArgumentException($"'{enumerationType}' does not derive from Enumeration<,>.", nameof(enumerationType));

        _enumerationType = enumerationType;
        _valueType = baseType.GetGenericArguments()[1];
        _getAllMethod = baseType.GetMethod("GetAll", BindingFlags.Public | BindingFlags.Static)!;
        _valueProperty = baseType.GetProperty("Value")!;
        _displayNameProperty = baseType.GetProperty("DisplayName")!;
    }

    /// <summary>
    /// When <see langword="true"/> (the default), a <see cref="string"/> value is also accepted if it matches an
    /// enumeration member's <c>DisplayName</c> (case-insensitively), in addition to its underlying value.
    /// </summary>
    public bool AllowName { get; set; } = true;

    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        object? convertedValue = null;
        var hasConvertedValue = false;
        try
        {
            convertedValue = Convert.ChangeType(value, _valueType, CultureInfo.InvariantCulture);
            hasConvertedValue = true;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
        }

        var allMembers = (IEnumerable)_getAllMethod.Invoke(null, null)!;
        foreach (var member in allMembers)
        {
            if (hasConvertedValue && Equals(_valueProperty.GetValue(member), convertedValue))
            {
                return ValidationResult.Success;
            }

            if (AllowName && value is string text)
            {
                var displayName = (string)_displayNameProperty.GetValue(member)!;
                if (string.Equals(displayName, text, StringComparison.OrdinalIgnoreCase))
                {
                    return ValidationResult.Success;
                }
            }
        }

        var memberNames = validationContext.MemberName is null ? null : new[] { validationContext.MemberName };
        return new ValidationResult(FormatErrorMessage(validationContext.DisplayName), memberNames);
    }

    /// <inheritdoc />
    public override string FormatErrorMessage(string name)
    {
        return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, _enumerationType.Name);
    }

    private static Type? FindEnumerationBaseType(Type enumerationType)
    {
        for (var type = enumerationType; type is not null && type != typeof(object); type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Enumeration<,>))
            {
                return type;
            }
        }

        return null;
    }
}
