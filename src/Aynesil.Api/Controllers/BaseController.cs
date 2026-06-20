using Aynesil.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Base controller for all API controllers.
/// Enforces [Authorize] by default — explicit [AllowAnonymous] required for public endpoints.
/// Provides Send() helper to dispatch MediatR commands/queries.
/// Returns ApiResponse<T> envelopes consistently.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    private ISender? _sender;
    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult OkResult<T>(T data, string? message = null) =>
        Ok(ApiResponse<T>.Ok(data, message));

    protected IActionResult CreatedResult<T>(T data, string location, string? message = null) =>
        Created(location, ApiResponse<T>.Ok(data, message));

    protected IActionResult NoContentResult(string? message = null) =>
        Ok(ApiResponse.OkNoContent(message));
}
