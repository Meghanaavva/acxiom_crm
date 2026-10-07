using AcxiomCRM.Api.Dtos;

namespace AcxiomCRM.Api.ViewModels;

// View models reuse the validated request DTOs so client and server use the same DataAnnotations

public class LoginViewModel : LoginRequest
{
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel : RegisterRequest { }

public class ChangePasswordViewModel : ChangePasswordRequest { }
