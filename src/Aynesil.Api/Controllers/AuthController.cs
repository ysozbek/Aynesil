using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Auth.Commands;
using Aynesil.Application.Features.Users.Dtos;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Authentication and account lifecycle endpoints.
/// Unauthenticated endpoints: Login, Refresh, VerifyEmail, RequestPasswordReset, ConfirmPasswordReset.
/// Authenticated endpoints: Logout, Register (admin), ChangePassword.
/// Note: [AllowAnonymous] is on individual actions to avoid overriding [Authorize] on protected endpoints.
/// </summary>
[Route("api/auth")]
public class AuthController : BaseController
{
    // ── Public auth flow ──────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await Sender.Send(new LoginCommand(
            request.Username,
            request.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()), ct);

        return OkResult(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await Sender.Send(new RefreshTokenCommand(
            request.RefreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()), ct);

        return OkResult(result);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        await Sender.Send(new LogoutCommand(request.RefreshToken), ct);
        return NoContentResult("Logged out successfully.");
    }

    // ── Registration & invite flow ────────────────────────────────────────────

    /// <summary>
    /// Admin creates a new user account with an email invite.
    /// If an email address is provided, the account is created as 'invited' and a
    /// verification email is dispatched. Without an email, the account is activated immediately.
    /// </summary>
    [HttpPost("register")]
    [HasPermission(Permissions.Users.Create)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await Sender.Send(new RegisterCommand(
            request.Username, request.FullName, request.Email, request.Phone,
            request.Password, request.PreferredLocale, request.PrimaryCampusId,
            request.SendVerificationEmail), ct);

        return CreatedResult(result, $"/api/users/{result.Id}");
    }

    /// <summary>
    /// Verifies an email address using the one-time token sent during registration.
    /// Transitions the user account from 'invited' → 'active'.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        await Sender.Send(new VerifyEmailCommand(request.Token), ct);
        return NoContentResult("Email verified successfully.");
    }

    // ── Password reset flow ────────────────────────────────────────────────────

    /// <summary>
    /// Initiates password reset for the account registered with the given email.
    /// Always returns 200 to prevent email enumeration (OWASP).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("request-password-reset")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestPasswordReset(
        [FromBody] RequestPasswordResetRequest request, CancellationToken ct)
    {
        await Sender.Send(new RequestPasswordResetCommand(request.Email), ct);
        return NoContentResult("If an account with that email exists, a reset link has been sent.");
    }

    /// <summary>
    /// Completes the password reset flow. The one-time token is validated,
    /// the new password is set, and all active sessions are revoked.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("confirm-password-reset")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmPasswordReset(
        [FromBody] ConfirmPasswordResetRequest request, CancellationToken ct)
    {
        await Sender.Send(new ConfirmPasswordResetCommand(request.Token, request.NewPassword), ct);
        return NoContentResult("Password reset successfully.");
    }

    // ── Authenticated password management ─────────────────────────────────────

    /// <summary>
    /// Changes the authenticated user's own password.
    /// Requires the current password for verification. All active sessions are revoked.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await Sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), ct);
        return NoContentResult("Password changed. Please log in again.");
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record RegisterRequest(
    string Username,
    string FullName,
    string? Email,
    string? Phone,
    string? Password,
    string? PreferredLocale,
    Guid? PrimaryCampusId,
    bool SendVerificationEmail = true);
public record VerifyEmailRequest(string Token);
public record RequestPasswordResetRequest(string Email);
public record ConfirmPasswordResetRequest(string Token, string NewPassword);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
