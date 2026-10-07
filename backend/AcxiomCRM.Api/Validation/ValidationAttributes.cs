using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Api.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AcxiomCRM.Api.Validation;

// Reusable data annotation attributes so all DTOs validate the same way.
// Empty values are allowed here - combine with [Required] when the field is mandatory.

// IClientModelValidator adds the data-val-* attributes used by jQuery unobtrusive validation in the Razor views

public class ValidEmailAttribute : RegularExpressionAttribute, IClientModelValidator
{
    public ValidEmailAttribute() : base(ValidationRules.EmailPattern)
    {
        ErrorMessage = "Enter a valid email address.";
    }

    public void AddValidation(ClientModelValidationContext context) => ClientRules.AddRegex(context, Pattern, ErrorMessage!);
}

public class ValidPhoneAttribute : RegularExpressionAttribute, IClientModelValidator
{
    public ValidPhoneAttribute() : base(ValidationRules.PhonePattern)
    {
        ErrorMessage = "Enter a valid phone number.";
    }

    public void AddValidation(ClientModelValidationContext context) => ClientRules.AddRegex(context, Pattern, ErrorMessage!);
}

internal static class ClientRules
{
    public static void Add(ClientModelValidationContext context, string key, string value)
    {
        context.Attributes.TryAdd(key, value);
    }

    public static void AddRegex(ClientModelValidationContext context, string pattern, string message)
    {
        Add(context, "data-val", "true");
        Add(context, "data-val-regex", message);
        Add(context, "data-val-regex-pattern", pattern);
    }
}

// Value must be one of the given list, e.g. [OneOf(typeof(LeadSources))] uses LeadSources.All
public class OneOfAttribute : ValidationAttribute
{
    private readonly string[] _allowed;

    public OneOfAttribute(Type constantsClass)
    {
        _allowed = (string[])constantsClass.GetField("All")!.GetValue(null)!;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is null || (value is string s && string.IsNullOrWhiteSpace(s))) return ValidationResult.Success;
        if (value is string text && _allowed.Contains(text)) return ValidationResult.Success;
        var label = context.DisplayName;
        return new ValidationResult(ErrorMessage ?? $"{label} must be one of: {string.Join(", ", _allowed)}.", new[] { context.MemberName! });
    }
}

// Max length with the same message format as the React app
public class MaxLenAttribute : StringLengthAttribute, IClientModelValidator
{
    public MaxLenAttribute(int max, string label) : base(max)
    {
        ErrorMessage = $"{label} cannot be more than {max} characters.";
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        ClientRules.Add(context, "data-val", "true");
        ClientRules.Add(context, "data-val-length", ErrorMessage!);
        ClientRules.Add(context, "data-val-length-max", MaximumLength.ToString());
        context.Attributes.TryAdd("maxlength", MaximumLength.ToString());
    }
}
