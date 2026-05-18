using System.Security.Claims;
using lms_api.Data;
using lms_api.DTOs.Auth;
using lms_api.Models;
using lms_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lms_api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>Register via invite token.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        Invite? invite = await _db.Invites
            .Include(i => i.Course)
            .FirstOrDefaultAsync(i => i.Token == request.InviteToken && !i.IsUsed && i.ExpiresAt > DateTime.UtcNow);

        if (invite == null)
            return BadRequest(new { message = "Invalid or expired invite token." });

        ApplicationUser? existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return Conflict(new { message = "An account with this email already exists." });

        ApplicationUser user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true
        };

        IdentityResult result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, "Learner");

        // Create enrollment for the invited course
        Enrollment enrollment = new Enrollment
        {
            UserId = user.Id,
            CourseId = invite.CourseId,
            Status = EnrollmentStatus.Invited,
            InvitedAt = DateTime.UtcNow
        };
        _db.Enrollments.Add(enrollment);

        invite.IsUsed = true;
        await _db.SaveChangesAsync();

        IList<string> roles = await _userManager.GetRolesAsync(user);
        string accessToken = _tokenService.GenerateAccessToken(user, roles);
        string refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token on user record via a claim or separate table
        // For simplicity we store it as a security stamp extension via tokens
        await _userManager.SetAuthenticationTokenAsync(user, "LmsApi", "RefreshToken", refreshToken);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsAdmin = user.IsAdmin
        });
    }

    /// <summary>Login with email and password.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        ApplicationUser? user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid credentials." });

        bool passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            return Unauthorized(new { message = "Invalid credentials." });

        IList<string> roles = await _userManager.GetRolesAsync(user);
        string accessToken = _tokenService.GenerateAccessToken(user, roles);
        string refreshToken = _tokenService.GenerateRefreshToken();

        await _userManager.SetAuthenticationTokenAsync(user, "LmsApi", "RefreshToken", refreshToken);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsAdmin = user.IsAdmin
        });
    }

    /// <summary>Exchange a Google ID token for an LMS access token.</summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleAuth([FromBody] GoogleAuthRequest request)
    {
        // Validate the Google ID token via Google's tokeninfo endpoint
        using HttpClient httpClient = new HttpClient();
        HttpResponseMessage response = await httpClient.GetAsync(
            $"https://oauth2.googleapis.com/tokeninfo?id_token={request.IdToken}");

        if (!response.IsSuccessStatusCode)
            return Unauthorized(new { message = "Invalid Google token." });

        System.Text.Json.JsonDocument json = System.Text.Json.JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        string? email = json.RootElement.GetProperty("email").GetString();
        string? firstName = json.RootElement.TryGetProperty("given_name", out System.Text.Json.JsonElement fnEl)
            ? fnEl.GetString() : null;
        string? lastName = json.RootElement.TryGetProperty("family_name", out System.Text.Json.JsonElement lnEl)
            ? lnEl.GetString() : null;

        if (string.IsNullOrEmpty(email))
            return Unauthorized(new { message = "Could not retrieve email from Google token." });

        ApplicationUser? user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName ?? string.Empty,
                LastName = lastName ?? string.Empty,
                EmailConfirmed = true
            };
            IdentityResult createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });

            await _userManager.AddToRoleAsync(user, "Learner");
        }

        IList<string> roles = await _userManager.GetRolesAsync(user);
        string accessToken = _tokenService.GenerateAccessToken(user, roles);
        string refreshToken = _tokenService.GenerateRefreshToken();

        await _userManager.SetAuthenticationTokenAsync(user, "LmsApi", "RefreshToken", refreshToken);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsAdmin = user.IsAdmin
        });
    }

    /// <summary>Exchange a Microsoft access token for an LMS access token.</summary>
    [HttpPost("microsoft")]
    public async Task<ActionResult<AuthResponse>> MicrosoftAuth([FromBody] MicrosoftAuthRequest request)
    {
        using HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.AccessToken);

        HttpResponseMessage response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");

        if (!response.IsSuccessStatusCode)
            return Unauthorized(new { message = "Invalid Microsoft token." });

        System.Text.Json.JsonDocument json = System.Text.Json.JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        string? email = json.RootElement.TryGetProperty("mail", out System.Text.Json.JsonElement mailEl)
            ? mailEl.GetString()
            : json.RootElement.TryGetProperty("userPrincipalName", out System.Text.Json.JsonElement upnEl)
                ? upnEl.GetString()
                : null;

        string? firstName = json.RootElement.TryGetProperty("givenName", out System.Text.Json.JsonElement fnEl)
            ? fnEl.GetString() : null;
        string? lastName = json.RootElement.TryGetProperty("surname", out System.Text.Json.JsonElement lnEl)
            ? lnEl.GetString() : null;

        if (string.IsNullOrEmpty(email))
            return Unauthorized(new { message = "Could not retrieve email from Microsoft token." });

        ApplicationUser? user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName ?? string.Empty,
                LastName = lastName ?? string.Empty,
                EmailConfirmed = true
            };
            IdentityResult createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });

            await _userManager.AddToRoleAsync(user, "Learner");
        }

        IList<string> roles = await _userManager.GetRolesAsync(user);
        string accessToken = _tokenService.GenerateAccessToken(user, roles);
        string refreshToken = _tokenService.GenerateRefreshToken();

        await _userManager.SetAuthenticationTokenAsync(user, "LmsApi", "RefreshToken", refreshToken);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsAdmin = user.IsAdmin
        });
    }

    /// <summary>Send password reset link to email.</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // Always return 200 to avoid leaking whether an account exists
        ApplicationUser? user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null)
        {
            string token = await _userManager.GeneratePasswordResetTokenAsync(user);
            string frontendUrl = _logger.IsEnabled(LogLevel.Debug)
                ? "http://localhost:5173"
                : "http://localhost:5173";
            string resetUrl = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(request.Email)}";
            _logger.LogInformation("Password reset link for {Email}: {Url}", request.Email, resetUrl);
            // TODO: send email via IEmailService when SMTP is configured
        }

        return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
    }

    /// <summary>Refresh access token using a refresh token.</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        // Find user by refresh token
        // We stored it via SetAuthenticationTokenAsync
        // We need to find which user has this refresh token
        ApplicationUser? user = _db.Users.AsEnumerable().FirstOrDefault(u =>
            _userManager.GetAuthenticationTokenAsync(u, "LmsApi", "RefreshToken").GetAwaiter().GetResult()
            == request.RefreshToken);

        if (user == null)
            return Unauthorized(new { message = "Invalid refresh token." });

        IList<string> roles = await _userManager.GetRolesAsync(user);
        string newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        string newRefreshToken = _tokenService.GenerateRefreshToken();

        await _userManager.SetAuthenticationTokenAsync(user, "LmsApi", "RefreshToken", newRefreshToken);

        return Ok(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsAdmin = user.IsAdmin
        });
    }
}
