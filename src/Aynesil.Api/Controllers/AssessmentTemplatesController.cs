using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Assessment.Commands;
using Aynesil.Application.Features.Assessment.Dtos;
using Aynesil.Application.Features.Assessment.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Assessment template management — CRUD, versioning, translations, sections, and items.
/// Route: /api/assessment-templates
///
/// Template lifecycle:
///   Create → (add sections/items) → Activate → (use in sessions)
///   → (when changes needed) → CreateVersion → (add updated sections/items to new version)
///
/// Platform templates (corporation_id = NULL) are visible to all tenants.
/// Tenant templates (corporation_id set) are scoped to one corporation.
/// </summary>
[Route("api/assessment-templates")]
public sealed class AssessmentTemplatesController : BaseController
{
    // ── Template Queries ──────────────────────────────────────────────────────

    /// <summary>Returns a paginated, filterable list of assessment templates.</summary>
    [HttpGet]
    [HasPermission(Permissions.AssessmentTemplates.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<AssessmentTemplateListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? typeId = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetAssessmentTemplatesQuery
        {
            CorporationId = corporationId,
            TypeId        = typeId,
            CategoryId    = categoryId,
            IsActive      = isActive,
            Page          = page,
            PageSize      = pageSize,
            Search        = search,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);

        return OkResult(result);
    }

    /// <summary>Returns the full detail of one template including sections, items, and translations.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.AssessmentTemplates.Read)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetAssessmentTemplateQuery(id), ct);
        return OkResult(result);
    }

    // ── Template Commands ─────────────────────────────────────────────────────

    /// <summary>Creates a new assessment template with optional initial translations.</summary>
    [HttpPost]
    [HasPermission(Permissions.AssessmentTemplates.Create)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssessmentTemplateRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateAssessmentTemplateCommand(
            req.CorporationId, req.Code, req.Name,
            req.TypeId, req.CategoryId, req.ScoringModel,
            req.Translations?.Select(t =>
                new CreateTemplateTranslationRequest(t.Locale, t.Name, t.Description))
                .ToList().AsReadOnly()), ct);

        return CreatedResult(result, $"/api/assessment-templates/{result.Id}");
    }

    /// <summary>Updates the template's metadata (name, type, category, scoring model).</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.AssessmentTemplates.Update)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateAssessmentTemplateRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateAssessmentTemplateCommand(
            id, req.Name, req.TypeId, req.CategoryId, req.ScoringModel, req.RowVersion), ct);

        return OkResult(result);
    }

    /// <summary>Activates or deactivates a template. Deactivated templates cannot be used in new sessions.</summary>
    [HttpPatch("{id:guid}/active")]
    [HasPermission(Permissions.AssessmentTemplates.Publish)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActive(
        Guid id, [FromBody] SetTemplateActiveRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new SetAssessmentTemplateActiveCommand(id, req.IsActive, req.RowVersion), ct);

        return OkResult(result);
    }

    /// <summary>
    /// Forks the template into a new version (version + 1, active).
    /// The source version is deactivated. Sections and items are not copied — add them
    /// to the new version using the sections/items endpoints.
    /// </summary>
    [HttpPost("{id:guid}/version")]
    [HasPermission(Permissions.AssessmentTemplates.Version)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateVersion(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateAssessmentTemplateVersionCommand(id), ct);
        return CreatedResult(result, $"/api/assessment-templates/{result.Id}");
    }

    /// <summary>Adds or updates the display text for a specific locale.</summary>
    [HttpPut("{id:guid}/translations/{locale}")]
    [HasPermission(Permissions.AssessmentTemplates.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpsertTranslation(
        Guid id, string locale,
        [FromBody] UpsertTranslationRequest req,
        CancellationToken ct)
    {
        await Sender.Send(
            new UpsertTemplateTranslationCommand(id, locale, req.Name, req.Description), ct);
        return NoContentResult("Translation saved.");
    }

    // ── Section Commands ──────────────────────────────────────────────────────

    /// <summary>Adds a new section to the template.</summary>
    [HttpPost("{id:guid}/sections")]
    [HasPermission(Permissions.AssessmentTemplates.Update)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddSection(
        Guid id, [FromBody] AddSectionRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AddAssessmentSectionCommand(
            id, req.Code, req.SortOrder, req.DevelopmentAreaId), ct);

        return CreatedResult(result, $"/api/assessment-templates/{id}");
    }

    /// <summary>Updates an existing section's code, sort order, and development area.</summary>
    [HttpPut("sections/{sectionId:guid}")]
    [HasPermission(Permissions.AssessmentTemplates.Update)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentSectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSection(
        Guid sectionId, [FromBody] UpdateSectionRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateAssessmentSectionCommand(
            sectionId, req.Code, req.SortOrder, req.DevelopmentAreaId), ct);

        return OkResult(result);
    }

    /// <summary>Removes a section and all its items (cascade).</summary>
    [HttpDelete("sections/{sectionId:guid}")]
    [HasPermission(Permissions.AssessmentTemplates.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSection(Guid sectionId, CancellationToken ct)
    {
        await Sender.Send(new DeleteAssessmentSectionCommand(sectionId), ct);
        return NoContentResult("Section deleted.");
    }

    // ── Item Commands ─────────────────────────────────────────────────────────

    /// <summary>Adds a new item to the specified section.</summary>
    [HttpPost("sections/{sectionId:guid}/items")]
    [HasPermission(Permissions.AssessmentTemplates.Update)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddItem(
        Guid sectionId, [FromBody] AddItemRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AddAssessmentItemCommand(
            sectionId, req.Code, req.Prompt, req.ResponseType,
            req.Choices, req.Weight, req.SortOrder), ct);

        return CreatedResult(result, $"/api/assessment-templates/items/{result.Id}");
    }

    /// <summary>Updates an existing item's prompt, response type, choices, and weight.</summary>
    [HttpPut("items/{itemId:guid}")]
    [HasPermission(Permissions.AssessmentTemplates.Update)]
    [ProducesResponseType(typeof(ApiResponse<AssessmentItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(
        Guid itemId, [FromBody] UpdateItemRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateAssessmentItemCommand(
            itemId, req.Code, req.Prompt, req.ResponseType,
            req.Choices, req.Weight, req.SortOrder), ct);

        return OkResult(result);
    }

    /// <summary>Removes an item from its section.</summary>
    [HttpDelete("items/{itemId:guid}")]
    [HasPermission(Permissions.AssessmentTemplates.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid itemId, CancellationToken ct)
    {
        await Sender.Send(new DeleteAssessmentItemCommand(itemId), ct);
        return NoContentResult("Item deleted.");
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record CreateAssessmentTemplateRequest(
    Guid? CorporationId,
    string Code,
    string Name,
    Guid? TypeId,
    Guid? CategoryId,
    string? ScoringModel,
    IReadOnlyList<TranslationRequest>? Translations);

public record TranslationRequest(string Locale, string Name, string? Description);

public record UpdateAssessmentTemplateRequest(
    string Name,
    Guid? TypeId,
    Guid? CategoryId,
    string? ScoringModel,
    int RowVersion);

public record SetTemplateActiveRequest(bool IsActive, int RowVersion);

public record UpsertTranslationRequest(string Name, string? Description);

public record AddSectionRequest(
    string Code,
    int SortOrder,
    Guid? DevelopmentAreaId);

public record UpdateSectionRequest(
    string Code,
    int SortOrder,
    Guid? DevelopmentAreaId);

public record AddItemRequest(
    string Code,
    string Prompt,
    string ResponseType,
    string? Choices,
    decimal Weight,
    int SortOrder);

public record UpdateItemRequest(
    string Code,
    string Prompt,
    string ResponseType,
    string? Choices,
    decimal Weight,
    int SortOrder);
