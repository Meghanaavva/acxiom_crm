using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Helpers;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Api.Services;

public static class IdentityErrorHelper
{
    // Converts IdentityResult errors into our { field, message } format.
    // passwordField lets the caller choose which form field password errors belong to.
    public static AppException ToException(IdentityResult result, string passwordField = "password")
    {
        var errors = result.Errors.Select(e =>
        {
            var field = e.Code switch
            {
                "DuplicateEmail" or "DuplicateUserName" or "InvalidEmail" => "email",
                "PasswordMismatch" => "currentPassword",
                _ when e.Code.StartsWith("Password") => passwordField,
                _ => "general",
            };
            return new FieldError(field, e.Description);
        }).GroupBy(e => e.Field).Select(g => g.First()).ToList();

        var isDuplicate = result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName");
        return isDuplicate
            ? AppException.Conflict("An account with this email already exists.", errors)
            : AppException.Fields(errors);
    }
}
