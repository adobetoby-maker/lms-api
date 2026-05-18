using System.ComponentModel.DataAnnotations;

namespace lms_api.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
