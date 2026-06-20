using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Leads.Commands;
using Aynesil.Application.Features.Leads.Dtos;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Interview lifecycle management.
/// Interviews are created via POST /api/leads/{id}/interviews.
/// This controller handles state transitions on existing interviews.
/// Route: /api/interviews
/// </summary>
[Route("api/interviews")]
public sealed class InterviewsController : BaseController
{
    /// <summary>Marks the interview as completed with outcome and recommendation notes.</summary>
    [HttpPost("{id:guid}/complete")]
    [HasPermission(Permissions.Interviews.Manage)]
    [ProducesResponseType(typeof(ApiResponse<InterviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteInterviewRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new CompleteInterviewCommand(id, req.Outcome, req.Recommendation, req.ConductedBy, req.RowVersion), ct);
        return OkResult(result);
    }

    /// <summary>Cancels the interview. Completed interviews cannot be cancelled.</summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Interviews.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] RowVersionRequest req, CancellationToken ct)
    {
        await Sender.Send(new CancelInterviewCommand(id, req.RowVersion), ct);
        return NoContentResult("Interview cancelled.");
    }

    /// <summary>Reschedules the interview to a new date/time.</summary>
    [HttpPost("{id:guid}/reschedule")]
    [HasPermission(Permissions.Interviews.Update)]
    [ProducesResponseType(typeof(ApiResponse<InterviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleInterviewRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new RescheduleInterviewCommand(id, req.NewScheduledAt, req.RowVersion), ct);
        return OkResult(result);
    }

    /// <summary>Records the prospect as a no-show for the interview.</summary>
    [HttpPost("{id:guid}/no-show")]
    [HasPermission(Permissions.Interviews.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MarkNoShow(Guid id, [FromBody] RowVersionRequest req, CancellationToken ct)
    {
        await Sender.Send(new MarkInterviewNoShowCommand(id, req.RowVersion), ct);
        return NoContentResult("Interview marked as no-show.");
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record CompleteInterviewRequest(
    string? Outcome,
    string? Recommendation,
    Guid? ConductedBy,
    int RowVersion);

public record RescheduleInterviewRequest(DateTimeOffset NewScheduledAt, int RowVersion);

/// <summary>Minimal concurrency-safe request body for state transitions that carry no extra data.</summary>
public record RowVersionRequest(int RowVersion);
