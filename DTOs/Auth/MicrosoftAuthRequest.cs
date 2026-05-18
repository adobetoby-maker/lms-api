using System.ComponentModel.DataAnnotations;

namespace lms_api.DTOs.Auth;

public class MicrosoftAuthRequest
{
    /// <summary>Microsoft access token from the frontend MSAL flow.</summary>
    [Required]
    public string AccessToken { get; set; } = string.Empty;
}
