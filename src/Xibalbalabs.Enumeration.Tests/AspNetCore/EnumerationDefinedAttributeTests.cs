using System.ComponentModel.DataAnnotations;
using Xibalbalabs.Enumeration.AspNetCore;
using Xibalbalabs.Enumeration.Tests.TestFixtures;
using Xunit;

namespace Xibalbalabs.Enumeration.Tests.AspNetCore;

public sealed class EnumerationDefinedAttributeTests
{
    private static ValidationResult? Validate(EnumerationDefinedAttribute attribute, object? value)
    {
        var context = new ValidationContext(new object())
        {
            MemberName = "StatusId",
            DisplayName = "StatusId",
        };

        return attribute.GetValidationResult(value, context);
    }

    [Fact]
    public void IsValid_ReturnsSuccess_ForValidUnderlyingValue()
    {
        var attribute = new EnumerationDefinedAttribute(typeof(Colour));

        var result = Validate(attribute, 2);

        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_ReturnsSuccess_ForValidDisplayNameString_WhenAllowNameEnabled()
    {
        var attribute = new EnumerationDefinedAttribute(typeof(Colour)) { AllowName = true };

        var result = Validate(attribute, "red");

        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_ReturnsError_ForDisplayNameString_WhenAllowNameDisabled()
    {
        var attribute = new EnumerationDefinedAttribute(typeof(Colour)) { AllowName = false };

        var result = Validate(attribute, "Red");

        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_ReturnsError_ForUndefinedValue()
    {
        var attribute = new EnumerationDefinedAttribute(typeof(Colour));

        var result = Validate(attribute, 999);

        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("Colour", result!.ErrorMessage);
    }

    [Fact]
    public void IsValid_ReturnsSuccess_ForNull()
    {
        var attribute = new EnumerationDefinedAttribute(typeof(Colour));

        var result = Validate(attribute, null);

        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_SupportsStringBackedEnumerations()
    {
        var attribute = new EnumerationDefinedAttribute(typeof(CountryCode));

        Assert.Equal(ValidationResult.Success, Validate(attribute, "US"));
        Assert.Equal(ValidationResult.Success, Validate(attribute, "canada"));
        Assert.NotEqual(ValidationResult.Success, Validate(attribute, "XX"));
    }
}
