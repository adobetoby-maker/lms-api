using System.ComponentModel.DataAnnotations;

namespace lms_api.DTOs.Auth;

public class GoogleAuthRequest
{
    /// <summary>Google ID token from the frontend Google Sign-In flow.</summary>
    [Required]
    public string IdToken { get; set; } = string.Empty;
}
