using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Surveys.Commands;
using Aynesil.Application.Features.Surveys.Dtos;
using Aynesil.Application.Features.Surveys.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Survey and parent feedback management.
/// Route: /api/surveys
/// </summary>
[Route("api/surveys")]
public sealed class SurveysController : BaseController
{
    // ── Survey Definitions ────────────────────────────────────────────────────────

    /// <summary>List surveys (paginated, filterable by type/target/active).</summary>
    [HttpGet]
    [HasPermission(Permissions.Surveys.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<SurveyDefinitionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSurveys(
        [FromQuery] GetSurveysQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Get a single survey with all questions and options.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Surveys.Read)]
    [ProducesResponseType(typeof(ApiResponse<SurveyDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSurvey(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetSurveyQuery(id), ct));

    /// <summary>Create a new survey definition with questions and answer options.</summary>
    [HttpPost]
    [HasPermission(Permissions.Surveys.Create)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSurvey(
        [FromBody] CreateSurveyDefinitionCommand command, CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return CreatedResult(id, $"/api/surveys/{id}");
    }

    /// <summary>Update survey header fields (title, description, target, type, active).</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Surveys.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSurvey(
        Guid id, [FromBody] UpdateSurveyDefinitionCommand command, CancellationToken ct)
    {
        var cmd = command with { Id = id };
        await Sender.Send(cmd, ct);
        return NoContentResult();
    }

    /// <summary>Soft-delete a survey (deactivates if responses exist).</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Surveys.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSurvey(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteSurveyDefinitionCommand(id), ct);
        return NoContentResult();
    }

    // ── Responses ─────────────────────────────────────────────────────────────────

    /// <summary>List survey responses (admin view).</summary>
    [HttpGet("responses")]
    [HasPermission(Permissions.Surveys.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<SurveyResponseListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResponses(
        [FromQuery] GetSurveyResponsesQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Submit a completed survey response (guardian or educator).</summary>
    [HttpPost("responses")]
    [HasPermission(Permissions.Surveys.Respond)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitResponse(
        [FromBody] SubmitSurveyResponseCommand command, CancellationToken ct)
    {
        var id = await Sender.Send(command, ct);
        return CreatedResult(id, $"/api/surveys/responses/{id}");
    }

    // ── Parent Feedback ───────────────────────────────────────────────────────────

    /// <summary>List parent feedback (star-rating submissions).</summary>
    [HttpGet("parent-feedback")]
    [HasPermission(Permissions.ParentFeedback.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ParentFeedbackListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParentFeedback(
        [FromQuery] GetParentFeedbackQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Submit a parent feedback (star-rating + optional comment).</summary>
    [HttpPost("parent-feedback")]
    [HasPermission(Permissions.ParentFeedback.Create)]
    [ProducesResponseType(typeof(ApiResponse<ParentFeedbackDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateParentFeedback(
        [FromBody] CreateParentFeedbackCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/surveys/parent-feedback/{result.Id}");
    }
}
