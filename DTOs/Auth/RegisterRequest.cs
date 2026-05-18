using System.ComponentModel.DataAnnotations;

namespace lms_api.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Invite token from the invitation email.</summary>
    [Required]
    public string InviteToken { get; set; } = string.Empty;
}
