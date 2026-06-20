using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Students.Commands;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Application.Features.Students.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Student lifecycle and case management.
/// All endpoints are tenant-scoped and require RBAC permission checks.
/// Route: /api/students
/// </summary>
[Route("api/students")]
public sealed class StudentsController : BaseController
{
    // ── Student List &amp; Detail ─────────────────────────────────────────────────

    [HttpGet]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<StudentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? campusId = null,
        [FromQuery] Guid? statusId = null,
        [FromQuery] bool? hasLead = null,
        [FromQuery] DateOnly? birthDateFrom = null,
        [FromQuery] DateOnly? birthDateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetStudentsQuery
        {
            CorporationId = corporationId,
            CampusId      = campusId,
            StatusId      = statusId,
            HasLead       = hasLead,
            BirthDateFrom = birthDateFrom,
            BirthDateTo   = birthDateTo,
            Page          = page,
            PageSize      = pageSize,
            Search        = search,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetStudentQuery(id), ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}/summary")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<StudentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetStudentSummaryQuery(id), ct);
        return OkResult(result);
    }

    // ── Student Sub-resource Queries ──────────────────────────────────────────

    [HttpGet("{id:guid}/guardians")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StudentGuardianDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGuardians(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetStudentGuardiansQuery(id), ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}/status-history")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StudentStatusHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusHistory(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetStudentStatusHistoryQuery(id), ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}/campuses")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StudentCampusDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCampuses(
        Guid id,
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetStudentCampusesQuery(id, activeOnly), ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}/diagnoses")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DiagnosisDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiagnoses(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetDiagnosesQuery(id), ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}/developmental-profiles")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DevelopmentalProfileDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDevelopmentalProfiles(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetDevelopmentalProfilesQuery(id), ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}/case-notes")]
    [HasPermission(Permissions.CaseNotes.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CaseNoteDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaseNotes(
        Guid id,
        [FromQuery] bool includeConfidential = false,
        [FromQuery] string? noteType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetCaseNotesQuery
        {
            StudentId           = id,
            IncludeConfidential = includeConfidential
                && HttpContext.User.HasClaim("permission", Permissions.CaseNotes.ReadConfidential),
            NoteType            = noteType,
            Page                = page,
            PageSize            = pageSize
        }, ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}/medical-reports")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MedicalReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedicalReports(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetMedicalReportsQuery(id), ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}/development-reports")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DevelopmentReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDevelopmentReports(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetDevelopmentReportsQuery(id), ct);
        return OkResult(result);
    }

    [HttpGet("{id:guid}/external-reports")]
    [HasPermission(Permissions.Students.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ExternalInstitutionReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExternalReports(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetExternalInstitutionReportsQuery(id), ct);
        return OkResult(result);
    }

    // ── Student Commands ──────────────────────────────────────────────────────

    [HttpPost]
    [HasPermission(Permissions.Students.Create)]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateStudentCommand(
            req.CorporationId, req.FirstName, req.LastName,
            req.StudentNo, req.NationalId, req.BirthDate, req.Gender,
            req.PrimaryCampusId, req.StatusId, req.LeadId, req.Notes), ct);
        return CreatedResult(result, $"/api/students/{result.Id}");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Students.Update)]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateStudentCommand(
            id, req.FirstName, req.LastName, req.StudentNo,
            req.NationalId, req.BirthDate, req.Gender,
            req.PrimaryCampusId, req.Notes, req.RowVersion), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Students.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteStudentCommand(id), ct);
        return NoContentResult("Student deleted.");
    }

    // ── Workflow Commands ─────────────────────────────────────────────────────

    [HttpPost("{id:guid}/status")]
    [HasPermission(Permissions.Students.ChangeStatus)]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeStatus(
        Guid id, [FromBody] ChangeStudentStatusRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new ChangeStudentStatusCommand(id, req.NewStatusId, req.Reason, req.RowVersion), ct);
        return OkResult(result);
    }

    // ── Campus Commands ───────────────────────────────────────────────────────

    [HttpPost("{id:guid}/campuses")]
    [HasPermission(Permissions.Students.Update)]
    [ProducesResponseType(typeof(ApiResponse<StudentCampusDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> EnrollAtCampus(
        Guid id, [FromBody] EnrollAtCampusRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new EnrollStudentAtCampusCommand(id, req.CampusId, req.IsPrimary, req.ActiveFrom), ct);
        return CreatedResult(result, $"/api/students/{id}/campuses/{result.Id}");
    }

    [HttpPost("{id:guid}/transfer")]
    [HasPermission(Permissions.Students.Update)]
    [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Transfer(
        Guid id, [FromBody] TransferStudentRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new TransferStudentCommand(id, req.NewCampusId, req.TransferDate, req.RowVersion), ct);
        return OkResult(result);
    }

    [HttpPatch("{id:guid}/campuses/{enrollmentId:guid}/end")]
    [HasPermission(Permissions.Students.Update)]
    [ProducesResponseType(typeof(ApiResponse<StudentCampusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EndCampusEnrollment(
        Guid id, Guid enrollmentId,
        [FromBody] EndEnrollmentRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new EndCampusEnrollmentCommand(enrollmentId, req.EndDate), ct);
        return OkResult(result);
    }

    // ── Guardian Link Commands ────────────────────────────────────────────────

    [HttpPost("{id:guid}/guardians")]
    [HasPermission(Permissions.Guardians.Create)]
    [ProducesResponseType(typeof(ApiResponse<StudentGuardianDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> LinkGuardian(
        Guid id, [FromBody] LinkGuardianRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new LinkGuardianToStudentCommand(
            id, req.GuardianId, req.RelationshipId,
            req.IsPrimary, req.HasCustody, req.PortalAccess, req.FinancialResponsible), ct);
        return CreatedResult(result, $"/api/students/{id}/guardians");
    }

    [HttpPut("{id:guid}/guardians/{linkId:guid}")]
    [HasPermission(Permissions.Guardians.Update)]
    [ProducesResponseType(typeof(ApiResponse<StudentGuardianDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGuardianLink(
        Guid id, Guid linkId, [FromBody] UpdateGuardianLinkRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateGuardianLinkCommand(
            linkId, req.RelationshipId, req.IsPrimary,
            req.HasCustody, req.PortalAccess, req.FinancialResponsible), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}/guardians/{linkId:guid}")]
    [HasPermission(Permissions.Guardians.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnlinkGuardian(Guid id, Guid linkId, CancellationToken ct)
    {
        await Sender.Send(new UnlinkGuardianFromStudentCommand(linkId), ct);
        return NoContentResult("Guardian unlinked.");
    }

    // ── Emergency Contacts ────────────────────────────────────────────────────

    [HttpPut("{id:guid}/emergency-contacts")]
    [HasPermission(Permissions.Students.Update)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmergencyContactDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReplaceEmergencyContacts(
        Guid id, [FromBody] ReplaceEmergencyContactsRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(
            new ReplaceEmergencyContactsCommand(id, req.Contacts), ct);
        return OkResult(result);
    }

    // ── Developmental Profile ─────────────────────────────────────────────────

    [HttpPut("{id:guid}/developmental-profiles")]
    [HasPermission(Permissions.Students.Update)]
    [ProducesResponseType(typeof(ApiResponse<DevelopmentalProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertDevelopmentalProfile(
        Guid id, [FromBody] UpsertDevProfileRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpsertDevelopmentalProfileCommand(
            id, req.DevelopmentAreaId, req.Summary, req.Strengths, req.Needs, req.AssessedOn), ct);
        return OkResult(result);
    }

    // ── Diagnoses ─────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/diagnoses")]
    [HasPermission(Permissions.Students.Write)]
    [ProducesResponseType(typeof(ApiResponse<DiagnosisDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddDiagnosis(
        Guid id, [FromBody] AddDiagnosisRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AddDiagnosisCommand(
            id, req.CategoryId, req.IcdCode, req.Description,
            req.DiagnosedOn, req.DiagnosedBy, req.SourceFileId), ct);
        return CreatedResult(result, $"/api/students/{id}/diagnoses/{result.Id}");
    }

    [HttpPut("{id:guid}/diagnoses/{diagnosisId:guid}")]
    [HasPermission(Permissions.Students.Write)]
    [ProducesResponseType(typeof(ApiResponse<DiagnosisDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDiagnosis(
        Guid id, Guid diagnosisId, [FromBody] UpdateDiagnosisRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateDiagnosisCommand(
            diagnosisId, req.CategoryId, req.IcdCode, req.Description,
            req.DiagnosedOn, req.DiagnosedBy, req.SourceFileId, req.RowVersion), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}/diagnoses/{diagnosisId:guid}")]
    [HasPermission(Permissions.Students.Write)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteDiagnosis(Guid id, Guid diagnosisId, CancellationToken ct)
    {
        await Sender.Send(new DeleteDiagnosisCommand(diagnosisId), ct);
        return NoContentResult("Diagnosis deleted.");
    }

    // ── Medical Reports ───────────────────────────────────────────────────────

    [HttpPost("{id:guid}/medical-reports")]
    [HasPermission(Permissions.Students.Write)]
    [ProducesResponseType(typeof(ApiResponse<MedicalReportDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddMedicalReport(
        Guid id, [FromBody] AddMedicalReportRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AddMedicalReportCommand(
            id, req.Title, req.ReportDate, req.Issuer, req.Summary, req.FileId), ct);
        return CreatedResult(result, $"/api/students/{id}/medical-reports/{result.Id}");
    }

    [HttpPut("{id:guid}/medical-reports/{reportId:guid}")]
    [HasPermission(Permissions.Students.Write)]
    [ProducesResponseType(typeof(ApiResponse<MedicalReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMedicalReport(
        Guid id, Guid reportId, [FromBody] UpdateMedicalReportRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateMedicalReportCommand(
            reportId, req.Title, req.ReportDate, req.Issuer, req.Summary, req.FileId, req.RowVersion), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}/medical-reports/{reportId:guid}")]
    [HasPermission(Permissions.Students.Write)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteMedicalReport(Guid id, Guid reportId, CancellationToken ct)
    {
        await Sender.Send(new DeleteMedicalReportCommand(reportId), ct);
        return NoContentResult("Medical report deleted.");
    }

    // ── Development Reports ───────────────────────────────────────────────────

    [HttpPost("{id:guid}/development-reports")]
    [HasPermission(Permissions.Students.Write)]
    [ProducesResponseType(typeof(ApiResponse<DevelopmentReportDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddDevelopmentReport(
        Guid id, [FromBody] AddDevelopmentReportRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AddDevelopmentReportCommand(
            id, req.PeriodLabel, req.ReportDate, req.AuthoredBy, req.Content, req.FileId), ct);
        return CreatedResult(result, $"/api/students/{id}/development-reports/{result.Id}");
    }

    [HttpPut("{id:guid}/development-reports/{reportId:guid}")]
    [HasPermission(Permissions.Students.Write)]
    [ProducesResponseType(typeof(ApiResponse<DevelopmentReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDevelopmentReport(
        Guid id, Guid reportId, [FromBody] UpdateDevelopmentReportRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateDevelopmentReportCommand(
            reportId, req.PeriodLabel, req.ReportDate, req.AuthoredBy, req.Content, req.FileId, req.RowVersion), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}/development-reports/{reportId:guid}")]
    [HasPermission(Permissions.Students.Write)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteDevelopmentReport(Guid id, Guid reportId, CancellationToken ct)
    {
        await Sender.Send(new DeleteDevelopmentReportCommand(reportId), ct);
        return NoContentResult("Development report deleted.");
    }

    // ── External Institution Reports ──────────────────────────────────────────

    [HttpPost("{id:guid}/external-reports")]
    [HasPermission(Permissions.Students.Write)]
    [ProducesResponseType(typeof(ApiResponse<ExternalInstitutionReportDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddExternalReport(
        Guid id, [FromBody] AddExternalReportRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AddExternalInstitutionReportCommand(
            id, req.InstitutionName, req.InstitutionTypeId, req.ReportDate, req.Summary, req.FileId), ct);
        return CreatedResult(result, $"/api/students/{id}/external-reports/{result.Id}");
    }

    [HttpDelete("{id:guid}/external-reports/{reportId:guid}")]
    [HasPermission(Permissions.Students.Write)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteExternalReport(Guid id, Guid reportId, CancellationToken ct)
    {
        await Sender.Send(new DeleteExternalInstitutionReportCommand(reportId), ct);
        return NoContentResult("External institution report deleted.");
    }

    // ── Case Notes ────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/case-notes")]
    [HasPermission(Permissions.CaseNotes.Create)]
    [ProducesResponseType(typeof(ApiResponse<CaseNoteDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddCaseNote(
        Guid id, [FromBody] AddCaseNoteRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new AddCaseNoteCommand(
            id, req.NoteType, req.Body, req.IsConfidential, req.AuthoredBy), ct);
        return CreatedResult(result, $"/api/students/{id}/case-notes/{result.Id}");
    }

    [HttpPut("{id:guid}/case-notes/{noteId:guid}")]
    [HasPermission(Permissions.CaseNotes.Update)]
    [ProducesResponseType(typeof(ApiResponse<CaseNoteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCaseNote(
        Guid id, Guid noteId, [FromBody] UpdateCaseNoteRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new UpdateCaseNoteCommand(
            noteId, req.NoteType, req.Body, req.IsConfidential, req.RowVersion), ct);
        return OkResult(result);
    }

    [HttpDelete("{id:guid}/case-notes/{noteId:guid}")]
    [HasPermission(Permissions.CaseNotes.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCaseNote(Guid id, Guid noteId, CancellationToken ct)
    {
        await Sender.Send(new DeleteCaseNoteCommand(noteId), ct);
        return NoContentResult("Case note deleted.");
    }
}

// ── Request body records ──────────────────────────────────────────────────────

public record CreateStudentRequest(
    Guid CorporationId,
    string FirstName,
    string LastName,
    string? StudentNo,
    string? NationalId,
    DateOnly? BirthDate,
    string? Gender,
    Guid? PrimaryCampusId,
    Guid? StatusId,
    Guid? LeadId,
    string? Notes);

public record UpdateStudentRequest(
    string FirstName,
    string LastName,
    string? StudentNo,
    string? NationalId,
    DateOnly? BirthDate,
    string? Gender,
    Guid? PrimaryCampusId,
    string? Notes,
    int RowVersion);

public record ChangeStudentStatusRequest(Guid NewStatusId, string? Reason, int RowVersion);

public record EnrollAtCampusRequest(Guid CampusId, bool IsPrimary, DateOnly? ActiveFrom);

public record TransferStudentRequest(Guid NewCampusId, DateOnly? TransferDate, int RowVersion);

public record EndEnrollmentRequest(DateOnly? EndDate);

public record LinkGuardianRequest(
    Guid GuardianId,
    Guid? RelationshipId,
    bool IsPrimary,
    bool HasCustody,
    bool PortalAccess,
    bool FinancialResponsible);

public record UpdateGuardianLinkRequest(
    Guid? RelationshipId,
    bool IsPrimary,
    bool HasCustody,
    bool PortalAccess,
    bool FinancialResponsible);

public record ReplaceEmergencyContactsRequest(
    IReadOnlyList<EmergencyContactInput> Contacts);

public record UpsertDevProfileRequest(
    Guid? DevelopmentAreaId,
    string? Summary,
    string? Strengths,
    string? Needs,
    DateOnly? AssessedOn);

public record AddDiagnosisRequest(
    Guid? CategoryId,
    string? IcdCode,
    string? Description,
    DateOnly? DiagnosedOn,
    string? DiagnosedBy,
    Guid? SourceFileId);

public record UpdateDiagnosisRequest(
    Guid? CategoryId,
    string? IcdCode,
    string? Description,
    DateOnly? DiagnosedOn,
    string? DiagnosedBy,
    Guid? SourceFileId,
    int RowVersion);

public record AddMedicalReportRequest(
    string Title,
    DateOnly? ReportDate,
    string? Issuer,
    string? Summary,
    Guid? FileId);

public record UpdateMedicalReportRequest(
    string Title,
    DateOnly? ReportDate,
    string? Issuer,
    string? Summary,
    Guid? FileId,
    int RowVersion);

public record AddDevelopmentReportRequest(
    string? PeriodLabel,
    DateOnly? ReportDate,
    Guid? AuthoredBy,
    string? Content,
    Guid? FileId);

public record UpdateDevelopmentReportRequest(
    string? PeriodLabel,
    DateOnly? ReportDate,
    Guid? AuthoredBy,
    string? Content,
    Guid? FileId,
    int RowVersion);

public record AddExternalReportRequest(
    string InstitutionName,
    Guid? InstitutionTypeId,
    DateOnly? ReportDate,
    string? Summary,
    Guid? FileId);

public record AddCaseNoteRequest(
    string? NoteType,
    string Body,
    bool IsConfidential,
    Guid? AuthoredBy);

public record UpdateCaseNoteRequest(
    string? NoteType,
    string Body,
    bool IsConfidential,
    int RowVersion);
