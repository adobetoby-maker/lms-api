using System.ComponentModel.DataAnnotations;

namespace lms_api.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
