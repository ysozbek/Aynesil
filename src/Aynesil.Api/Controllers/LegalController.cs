using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Legal.Commands;
using Aynesil.Application.Features.Legal.Dtos;
using Aynesil.Application.Features.Legal.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Contract and Consent Management.
/// Route: /api/legal
///
/// Sub-resources:
///   /api/legal/contract-templates   — versioned contract templates
///   /api/legal/contracts            — student contract instances + workflow
///   /api/legal/consent-templates    — versioned KVKK/consent templates
///   /api/legal/consents             — student consent ledger (grant / withdraw)
///   /api/legal/reporting            — compliance reports
/// </summary>
[Route("api/legal")]
public sealed class LegalController : BaseController
{
    // ═══════════════════════════════════════════════════════════════════════════
    // CONTRACT TEMPLATES
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Paginated list of contract templates. Filter by type and IsCurrent flag.</summary>
    [HttpGet("contract-templates")]
    [HasPermission(Permissions.ContractTemplates.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ContractTemplateListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractTemplates(
        [FromQuery] GetContractTemplatesQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Full detail for a single contract template (includes translations).</summary>
    [HttpGet("contract-templates/{id:guid}")]
    [HasPermission(Permissions.ContractTemplates.Read)]
    [ProducesResponseType(typeof(ApiResponse<ContractTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContractTemplate(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetContractTemplateQuery(id), ct));

    /// <summary>
    /// All versions (including archived) for a given template code.
    /// Use for version history UI.
    /// </summary>
    [HttpGet("contract-templates/{corporationId:guid}/{code}/versions")]
    [HasPermission(Permissions.ContractTemplates.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ContractTemplateListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractTemplateVersions(
        Guid corporationId, string code, CancellationToken ct)
        => OkResult(await Sender.Send(new GetContractTemplateVersionsQuery(corporationId, code), ct));

    /// <summary>Create a new contract template (v1, is_current = true).</summary>
    [HttpPost("contract-templates")]
    [HasPermission(Permissions.ContractTemplates.Create)]
    [ProducesResponseType(typeof(ApiResponse<ContractTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContractTemplate(
        [FromBody] CreateContractTemplateCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/legal/contract-templates/{result.Id}");
    }

    /// <summary>Update the current version's metadata and/or translations.</summary>
    [HttpPut("contract-templates/{id:guid}")]
    [HasPermission(Permissions.ContractTemplates.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContractTemplate(
        Guid id, [FromBody] UpdateContractTemplateCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>Create a new version from the current template (archives current, returns new version).</summary>
    [HttpPost("contract-templates/{id:guid}/new-version")]
    [HasPermission(Permissions.ContractTemplates.Version)]
    [ProducesResponseType(typeof(ApiResponse<ContractTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NewVersionContractTemplate(
        Guid id, [FromBody] NewVersionContractTemplateCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { CurrentTemplateId = id }, ct);
        return CreatedResult(result, $"/api/legal/contract-templates/{result.Id}");
    }

    /// <summary>Soft-delete a contract template (only if no student contracts reference it).</summary>
    [HttpDelete("contract-templates/{id:guid}")]
    [HasPermission(Permissions.ContractTemplates.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteContractTemplate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteContractTemplateCommand(id), ct);
        return NoContentResult();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // STUDENT CONTRACTS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Paginated list of student contracts.
    /// Filterable by student, guardian, status, template.
    /// </summary>
    [HttpGet("contracts")]
    [HasPermission(Permissions.StudentContracts.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<StudentContractListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContracts(
        [FromQuery] GetStudentContractsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Full contract detail including signature data.</summary>
    [HttpGet("contracts/{id:guid}")]
    [HasPermission(Permissions.StudentContracts.Read)]
    [ProducesResponseType(typeof(ApiResponse<StudentContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContract(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetStudentContractQuery(id), ct));

    /// <summary>Generate a new contract for a student (starts in draft).</summary>
    [HttpPost("contracts")]
    [HasPermission(Permissions.StudentContracts.Generate)]
    [ProducesResponseType(typeof(ApiResponse<StudentContractDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateContract(
        [FromBody] GenerateStudentContractCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/legal/contracts/{result.Id}");
    }

    /// <summary>Update draft/sent contract details (dates, guardian).</summary>
    [HttpPut("contracts/{id:guid}")]
    [HasPermission(Permissions.StudentContracts.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContract(
        Guid id, [FromBody] UpdateStudentContractCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>Send the contract to the guardian for signature (draft → sent).</summary>
    [HttpPost("contracts/{id:guid}/send")]
    [HasPermission(Permissions.StudentContracts.Send)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendContract(Guid id, CancellationToken ct)
    {
        await Sender.Send(new SendStudentContractCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Record that the guardian has signed (sent → signed). Signature data is immutable after this.</summary>
    [HttpPost("contracts/{id:guid}/sign")]
    [HasPermission(Permissions.StudentContracts.Sign)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignContract(
        Guid id, [FromBody] SignStudentContractCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>Activate a signed contract (signed → active). Optionally attach the final signed file.</summary>
    [HttpPost("contracts/{id:guid}/activate")]
    [HasPermission(Permissions.StudentContracts.Activate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateContract(
        Guid id, [FromBody] ActivateStudentContractCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>Mark an active contract as expired (active → expired).</summary>
    [HttpPost("contracts/{id:guid}/expire")]
    [HasPermission(Permissions.StudentContracts.Expire)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExpireContract(Guid id, CancellationToken ct)
    {
        await Sender.Send(new ExpireStudentContractCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Forcefully terminate any non-terminal contract.</summary>
    [HttpPost("contracts/{id:guid}/terminate")]
    [HasPermission(Permissions.StudentContracts.Terminate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TerminateContract(Guid id, CancellationToken ct)
    {
        await Sender.Send(new TerminateStudentContractCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Soft-delete a draft contract.</summary>
    [HttpDelete("contracts/{id:guid}")]
    [HasPermission(Permissions.StudentContracts.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContract(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteStudentContractCommand(id), ct);
        return NoContentResult();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CONSENT TEMPLATES
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Paginated list of consent templates. Filter by type, IsCurrent, IsMandatory.</summary>
    [HttpGet("consent-templates")]
    [HasPermission(Permissions.ConsentTemplates.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ConsentTemplateListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConsentTemplates(
        [FromQuery] GetConsentTemplatesQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Full consent template detail (includes translations).</summary>
    [HttpGet("consent-templates/{id:guid}")]
    [HasPermission(Permissions.ConsentTemplates.Read)]
    [ProducesResponseType(typeof(ApiResponse<ConsentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsentTemplate(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetConsentTemplateQuery(id), ct));

    /// <summary>All versions (including archived) for a consent template code.</summary>
    [HttpGet("consent-templates/{corporationId:guid}/{code}/versions")]
    [HasPermission(Permissions.ConsentTemplates.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ConsentTemplateListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConsentTemplateVersions(
        Guid corporationId, string code, CancellationToken ct)
        => OkResult(await Sender.Send(new GetConsentTemplateVersionsQuery(corporationId, code), ct));

    /// <summary>Create a new consent template (v1).</summary>
    [HttpPost("consent-templates")]
    [HasPermission(Permissions.ConsentTemplates.Create)]
    [ProducesResponseType(typeof(ApiResponse<ConsentTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateConsentTemplate(
        [FromBody] CreateConsentTemplateCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/legal/consent-templates/{result.Id}");
    }

    /// <summary>Update the current version's metadata and/or translations.</summary>
    [HttpPut("consent-templates/{id:guid}")]
    [HasPermission(Permissions.ConsentTemplates.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConsentTemplate(
        Guid id, [FromBody] UpdateConsentTemplateCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    /// <summary>Create a new version (archives current, copies translations, returns new version).</summary>
    [HttpPost("consent-templates/{id:guid}/new-version")]
    [HasPermission(Permissions.ConsentTemplates.Version)]
    [ProducesResponseType(typeof(ApiResponse<ConsentTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NewVersionConsentTemplate(
        Guid id, [FromBody] NewVersionConsentTemplateCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command with { CurrentTemplateId = id }, ct);
        return CreatedResult(result, $"/api/legal/consent-templates/{result.Id}");
    }

    /// <summary>Soft-delete a consent template (only if no consent records reference it).</summary>
    [HttpDelete("consent-templates/{id:guid}")]
    [HasPermission(Permissions.ConsentTemplates.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConsentTemplate(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeleteConsentTemplateCommand(id), ct);
        return NoContentResult();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // STUDENT CONSENTS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Paginated consent ledger for a corporation.
    /// Filterable by student, consent type, state. Set includeExpired=true to see expired records.
    /// </summary>
    [HttpGet("consents")]
    [HasPermission(Permissions.StudentConsents.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<StudentConsentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConsents(
        [FromQuery] GetStudentConsentsQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>Full consent record detail (includes evidence file reference and template version).</summary>
    [HttpGet("consents/{id:guid}")]
    [HasPermission(Permissions.StudentConsents.Read)]
    [ProducesResponseType(typeof(ApiResponse<StudentConsentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsent(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetStudentConsentQuery(id), ct));

    /// <summary>
    /// Grant a consent for a student/guardian.
    /// Records the consent template version shown to the guardian (KVKK compliance).
    /// </summary>
    [HttpPost("consents/grant")]
    [HasPermission(Permissions.StudentConsents.Grant)]
    [ProducesResponseType(typeof(ApiResponse<StudentConsentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GrantConsent(
        [FromBody] GrantConsentCommand command, CancellationToken ct)
    {
        var result = await Sender.Send(command, ct);
        return CreatedResult(result, $"/api/legal/consents/{result.Id}");
    }

    /// <summary>
    /// Withdraw a previously granted consent.
    /// Immutable after withdrawal — re-grant creates a new consent record.
    /// </summary>
    [HttpPost("consents/{id:guid}/withdraw")]
    [HasPermission(Permissions.StudentConsents.Withdraw)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WithdrawConsent(Guid id, CancellationToken ct)
    {
        await Sender.Send(new WithdrawConsentCommand(id), ct);
        return NoContentResult();
    }

    /// <summary>Attach a signed/scanned evidence file to a granted consent.</summary>
    [HttpPost("consents/{id:guid}/evidence")]
    [HasPermission(Permissions.StudentConsents.AttachEvidence)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AttachConsentEvidence(
        Guid id, [FromBody] AttachConsentEvidenceCommand command, CancellationToken ct)
    {
        await Sender.Send(command with { Id = id }, ct);
        return NoContentResult();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // REPORTING
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Contract Report: contract status summary per student.
    /// Shows active/expired/terminated counts and date of latest signature.
    /// </summary>
    [HttpGet("reporting/contracts")]
    [HasPermission(Permissions.LegalReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ContractReportItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractReport(
        [FromQuery] GetContractReportQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>
    /// Consent Report: compliance summary per student per consent type.
    /// Flags missing mandatory consents for compliance gap detection.
    /// </summary>
    [HttpGet("reporting/consents")]
    [HasPermission(Permissions.LegalReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ConsentReportItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConsentReport(
        [FromQuery] GetConsentReportQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));

    /// <summary>
    /// Signature Report: digital-signature tracking.
    /// Lists contracts with signature method, provider ref, and whether a signed PDF is attached.
    /// </summary>
    [HttpGet("reporting/signatures")]
    [HasPermission(Permissions.LegalReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SignatureReportItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSignatureReport(
        [FromQuery] GetSignatureReportQuery query, CancellationToken ct)
        => OkResult(await Sender.Send(query, ct));
}
