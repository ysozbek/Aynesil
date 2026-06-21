using Aynesil.Api.Authorization;
using Aynesil.Application.Features.Finance.Commands;
using Aynesil.Application.Features.Finance.Dtos;
using Aynesil.Application.Features.Finance.Queries;
using Aynesil.Shared;
using Aynesil.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Aynesil.Api.Controllers;

/// <summary>
/// Payment &amp; Package Management — Package Definitions, Student Packages, Credits,
/// Invoices, Payments, Refunds, Discounts, Scholarships, Promotions, Finance Reports.
/// Route: /api/payments
/// </summary>
[Route("api/payments")]
public sealed class PaymentsController : BaseController
{
    // ══════════════════════════════════════════════════════════════════════════
    // PACKAGE DEFINITIONS
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("package-definitions")]
    [HasPermission(Permissions.PackageDefinitions.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PackageDefinitionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPackageDefinitions(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? packageTypeId = null,
        [FromQuery] Guid? programId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetPackageDefinitionsQuery
        {
            CorporationId = corporationId,
            PackageTypeId = packageTypeId,
            ProgramId     = programId,
            IsActive      = isActive,
            Page          = page,
            PageSize      = pageSize,
            Search        = search,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("package-definitions/{id:guid}")]
    [HasPermission(Permissions.PackageDefinitions.Read)]
    [ProducesResponseType(typeof(ApiResponse<PackageDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPackageDefinition(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetPackageDefinitionQuery(id), ct));

    [HttpPost("package-definitions")]
    [HasPermission(Permissions.PackageDefinitions.Create)]
    [ProducesResponseType(typeof(ApiResponse<PackageDefinitionDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePackageDefinition(
        [FromBody] CreatePackageDefinitionRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreatePackageDefinitionCommand(
            req.CorporationId, req.Code, req.Name, req.ListPrice,
            req.PackageTypeId, req.ProgramId,
            req.TotalCredits, req.ValidityDays, req.Currency), ct);
        return CreatedResult(result, $"/api/payments/package-definitions/{result.Id}");
    }

    [HttpPut("package-definitions/{id:guid}")]
    [HasPermission(Permissions.PackageDefinitions.Update)]
    [ProducesResponseType(typeof(ApiResponse<PackageDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePackageDefinition(
        Guid id, [FromBody] UpdatePackageDefinitionRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new UpdatePackageDefinitionCommand(
            id, req.Code, req.Name, req.ListPrice,
            req.PackageTypeId, req.ProgramId,
            req.TotalCredits, req.ValidityDays, req.Currency, req.RowVersion), ct));

    [HttpPost("package-definitions/{id:guid}/activate")]
    [HasPermission(Permissions.PackageDefinitions.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActivatePackageDefinition(Guid id, CancellationToken ct)
    {
        await Sender.Send(new ActivatePackageDefinitionCommand(id), ct);
        return NoContentResult("Package definition activated.");
    }

    [HttpPost("package-definitions/{id:guid}/deactivate")]
    [HasPermission(Permissions.PackageDefinitions.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivatePackageDefinition(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeactivatePackageDefinitionCommand(id), ct);
        return NoContentResult("Package definition deactivated.");
    }

    [HttpDelete("package-definitions/{id:guid}")]
    [HasPermission(Permissions.PackageDefinitions.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePackageDefinition(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeletePackageDefinitionCommand(id), ct);
        return NoContentResult("Package definition deleted.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STUDENT PACKAGES
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("packages")]
    [HasPermission(Permissions.StudentPackages.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<StudentPackageListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentPackages(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? packageDefinitionId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? expiringWithin30Days = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "desc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetStudentPackagesQuery
        {
            CorporationId        = corporationId,
            StudentId            = studentId,
            PackageDefinitionId  = packageDefinitionId,
            Status               = status,
            ExpiringWithin30Days = expiringWithin30Days,
            Page                 = page,
            PageSize             = pageSize,
            SortBy               = sortBy,
            SortDirection        = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("packages/{id:guid}")]
    [HasPermission(Permissions.StudentPackages.Read)]
    [ProducesResponseType(typeof(ApiResponse<StudentPackageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentPackage(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetStudentPackageQuery(id), ct));

    [HttpGet("packages/{id:guid}/balance")]
    [HasPermission(Permissions.StudentPackages.Read)]
    [ProducesResponseType(typeof(ApiResponse<PackageBalanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPackageBalance(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetPackageBalanceQuery(id), ct));

    [HttpPost("packages")]
    [HasPermission(Permissions.StudentPackages.Purchase)]
    [ProducesResponseType(typeof(ApiResponse<StudentPackageDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> PurchasePackage(
        [FromBody] PurchasePackageRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new PurchaseStudentPackageCommand(
            req.CorporationId, req.StudentId,
            req.TotalCredits, req.Price,
            req.PackageDefinitionId, req.ExpiresOn, req.Currency), ct);
        return CreatedResult(result, $"/api/payments/packages/{result.Id}");
    }

    [HttpPost("packages/{id:guid}/cancel")]
    [HasPermission(Permissions.StudentPackages.Cancel)]
    [ProducesResponseType(typeof(ApiResponse<StudentPackageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelPackage(
        Guid id, [FromBody] CancelPackageRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new CancelStudentPackageCommand(id, req.RowVersion), ct));

    // ══════════════════════════════════════════════════════════════════════════
    // CREDIT LEDGER
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("credits")]
    [HasPermission(Permissions.CreditLedger.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CreditLedgerEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCreditLedger(
        [FromQuery] Guid? studentPackageId = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? corporationId = null,
        [FromQuery] string? entryType = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetCreditLedgerQuery
        {
            StudentPackageId = studentPackageId,
            StudentId        = studentId,
            CorporationId    = corporationId,
            EntryType        = entryType,
            From             = from,
            To               = to,
            Page             = page,
            PageSize         = pageSize
        }, ct);
        return OkResult(result);
    }

    [HttpGet("credits/summary/{studentId:guid}")]
    [HasPermission(Permissions.CreditLedger.Read)]
    [ProducesResponseType(typeof(ApiResponse<CreditSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCreditSummary(
        Guid studentId,
        [FromQuery] Guid? corporationId = null,
        CancellationToken ct = default)
        => OkResult(await Sender.Send(new GetCreditSummaryQuery(studentId, corporationId), ct));

    [HttpPost("credits/consume")]
    [HasPermission(Permissions.CreditLedger.Consume)]
    [ProducesResponseType(typeof(ApiResponse<CreditLedgerEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConsumeCredits(
        [FromBody] ConsumeCreditsRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new ConsumeCreditsCommand(
            req.StudentPackageId, req.Amount, req.SessionId, req.Reason), ct));

    [HttpPost("credits/grant")]
    [HasPermission(Permissions.CreditLedger.Grant)]
    [ProducesResponseType(typeof(ApiResponse<CreditLedgerEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GrantBonusCredits(
        [FromBody] GrantCreditsRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new GrantBonusCreditsCommand(
            req.StudentPackageId, req.Amount, req.Reason), ct));

    [HttpPost("credits/refund")]
    [HasPermission(Permissions.CreditLedger.Grant)]
    [ProducesResponseType(typeof(ApiResponse<CreditLedgerEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefundCredits(
        [FromBody] RefundCreditsRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new RefundSessionCreditsCommand(
            req.StudentPackageId, req.Amount, req.Reason), ct));

    [HttpPost("credits/adjust")]
    [HasPermission(Permissions.CreditLedger.Adjust)]
    [ProducesResponseType(typeof(ApiResponse<CreditLedgerEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AdjustCredits(
        [FromBody] AdjustCreditsRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new AdjustCreditsCommand(
            req.StudentPackageId, req.Delta, req.Reason), ct));

    // ══════════════════════════════════════════════════════════════════════════
    // INVOICES
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("invoices")]
    [HasPermission(Permissions.Invoices.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<InvoiceListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? issuedFrom = null,
        [FromQuery] DateOnly? issuedTo = null,
        [FromQuery] DateOnly? dueFrom = null,
        [FromQuery] DateOnly? dueTo = null,
        [FromQuery] bool? overdue = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "desc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetInvoicesQuery
        {
            CorporationId = corporationId,
            StudentId     = studentId,
            Status        = status,
            IssuedFrom    = issuedFrom,
            IssuedTo      = issuedTo,
            DueFrom       = dueFrom,
            DueTo         = dueTo,
            Overdue       = overdue,
            Search        = search,
            Page          = page,
            PageSize      = pageSize,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("invoices/{id:guid}")]
    [HasPermission(Permissions.Invoices.Read)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetInvoiceQuery(id), ct));

    [HttpPost("invoices")]
    [HasPermission(Permissions.Invoices.Create)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInvoice(
        [FromBody] CreateInvoiceRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreateInvoiceCommand(
            req.CorporationId, req.IssueDate, req.Currency,
            req.StudentId, req.GuardianId, req.DueDate), ct);
        return CreatedResult(result, $"/api/payments/invoices/{result.Id}");
    }

    [HttpPost("invoices/{id:guid}/lines")]
    [HasPermission(Permissions.Invoices.Update)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddInvoiceLine(
        Guid id, [FromBody] AddInvoiceLineRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new AddInvoiceLineCommand(
            id, req.Description, req.UnitPrice,
            req.Quantity, req.StudentPackageId, req.SortOrder), ct));

    [HttpDelete("invoices/{id:guid}/lines/{lineId:guid}")]
    [HasPermission(Permissions.Invoices.Update)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveInvoiceLine(
        Guid id, Guid lineId, CancellationToken ct)
        => OkResult(await Sender.Send(new RemoveInvoiceLineCommand(id, lineId), ct));

    [HttpPost("invoices/{id:guid}/issue")]
    [HasPermission(Permissions.Invoices.Update)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> IssueInvoice(
        Guid id, [FromBody] IssueInvoiceRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new IssueInvoiceCommand(
            id, req.InvoiceNo, req.RowVersion), ct));

    [HttpPost("invoices/{id:guid}/void")]
    [HasPermission(Permissions.Invoices.Void)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VoidInvoice(
        Guid id, [FromBody] VoidInvoiceRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new VoidInvoiceCommand(id, req.RowVersion), ct));

    // ══════════════════════════════════════════════════════════════════════════
    // PAYMENTS (FINANCIAL TRANSACTIONS)
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("transactions")]
    [HasPermission(Permissions.FinancePayments.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PaymentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? invoiceId = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? paymentMethodId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTimeOffset? paidFrom = null,
        [FromQuery] DateTimeOffset? paidTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "desc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetPaymentsQuery
        {
            CorporationId   = corporationId,
            InvoiceId       = invoiceId,
            StudentId       = studentId,
            PaymentMethodId = paymentMethodId,
            Status          = status,
            PaidFrom        = paidFrom,
            PaidTo          = paidTo,
            Page            = page,
            PageSize        = pageSize,
            SortBy          = sortBy,
            SortDirection   = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("transactions/{id:guid}")]
    [HasPermission(Permissions.FinancePayments.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayment(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetPaymentQuery(id), ct));

    [HttpPost("transactions")]
    [HasPermission(Permissions.FinancePayments.Record)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordPayment(
        [FromBody] RecordPaymentRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new RecordPaymentCommand(
            req.CorporationId, req.Amount, req.Currency,
            req.InvoiceId, req.StudentId, req.PaymentMethodId,
            req.GatewayProviderId, req.GatewayReference, req.IdempotencyKey), ct);
        return CreatedResult(result, $"/api/payments/transactions/{result.Id}");
    }

    [HttpPost("transactions/{id:guid}/capture")]
    [HasPermission(Permissions.FinancePayments.Capture)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CapturePayment(
        Guid id, [FromBody] CapturePaymentRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new CapturePaymentCommand(
            id, req.GatewayReference, req.PaidAt, req.RowVersion), ct));

    [HttpPost("transactions/{id:guid}/fail")]
    [HasPermission(Permissions.FinancePayments.Capture)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FailPayment(
        Guid id, [FromBody] PaymentRowVersionRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new FailPaymentCommand(id, req.RowVersion), ct));

    // ══════════════════════════════════════════════════════════════════════════
    // REFUNDS
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("refunds")]
    [HasPermission(Permissions.Refunds.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<RefundDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRefunds(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? paymentId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "desc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetRefundsQuery
        {
            CorporationId = corporationId,
            PaymentId     = paymentId,
            Status        = status,
            From          = from,
            To            = to,
            Page          = page,
            PageSize      = pageSize,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("refunds/{id:guid}")]
    [HasPermission(Permissions.Refunds.Read)]
    [ProducesResponseType(typeof(ApiResponse<RefundDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRefund(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetRefundQuery(id), ct));

    [HttpPost("refunds")]
    [HasPermission(Permissions.Refunds.Request)]
    [ProducesResponseType(typeof(ApiResponse<RefundDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RequestRefund(
        [FromBody] RequestRefundRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new RequestRefundCommand(
            req.PaymentId, req.Amount, req.Reason), ct);
        return CreatedResult(result, $"/api/payments/refunds/{result.Id}");
    }

    [HttpPost("refunds/{id:guid}/process")]
    [HasPermission(Permissions.Refunds.Process)]
    [ProducesResponseType(typeof(ApiResponse<RefundDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessRefund(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new ProcessRefundCommand(id), ct));

    [HttpPost("refunds/{id:guid}/fail")]
    [HasPermission(Permissions.Refunds.Process)]
    [ProducesResponseType(typeof(ApiResponse<RefundDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FailRefund(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new FailRefundCommand(id), ct));

    // ══════════════════════════════════════════════════════════════════════════
    // DISCOUNTS
    // ══════════════════════════════════════════════════════════════════════════

    [HttpPost("discounts")]
    [HasPermission(Permissions.Discounts.Apply)]
    [ProducesResponseType(typeof(ApiResponse<DiscountDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> ApplyDiscount(
        [FromBody] ApplyDiscountRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new ApplyDiscountCommand(
            req.CorporationId, req.Value, req.IsPercentage,
            req.InvoiceId, req.StudentPackageId,
            req.DiscountTypeId, req.Reason), ct);
        return CreatedResult(result, null);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SCHOLARSHIPS
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("scholarships")]
    [HasPermission(Permissions.Scholarships.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ScholarshipListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScholarships(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? scholarshipTypeId = null,
        [FromQuery] bool? activeOn = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetScholarshipsQuery
        {
            CorporationId     = corporationId,
            StudentId         = studentId,
            ScholarshipTypeId = scholarshipTypeId,
            ActiveOn          = activeOn,
            Page              = page,
            PageSize          = pageSize,
            SortBy            = sortBy,
            SortDirection     = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("scholarships/{id:guid}")]
    [HasPermission(Permissions.Scholarships.Read)]
    [ProducesResponseType(typeof(ApiResponse<ScholarshipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScholarship(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetScholarshipQuery(id), ct));

    [HttpPost("scholarships")]
    [HasPermission(Permissions.Scholarships.Grant)]
    [ProducesResponseType(typeof(ApiResponse<ScholarshipDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> GrantScholarship(
        [FromBody] GrantScholarshipRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new GrantScholarshipCommand(
            req.CorporationId, req.StudentId,
            req.ScholarshipTypeId, req.Percentage, req.Amount,
            req.ValidFrom, req.ValidTo, req.Note), ct);
        return CreatedResult(result, $"/api/payments/scholarships/{result.Id}");
    }

    [HttpPut("scholarships/{id:guid}")]
    [HasPermission(Permissions.Scholarships.Update)]
    [ProducesResponseType(typeof(ApiResponse<ScholarshipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateScholarship(
        Guid id, [FromBody] UpdateScholarshipRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new UpdateScholarshipCommand(
            id, req.ScholarshipTypeId,
            req.Percentage, req.Amount,
            req.ValidFrom, req.ValidTo,
            req.Note, req.RowVersion), ct));

    // ══════════════════════════════════════════════════════════════════════════
    // PROMOTIONS
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("promotions")]
    [HasPermission(Permissions.Promotions.Read)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PromotionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPromotions(
        [FromQuery] Guid? corporationId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? validToday = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetPromotionsQuery
        {
            CorporationId = corporationId,
            IsActive      = isActive,
            ValidToday    = validToday,
            Search        = search,
            Page          = page,
            PageSize      = pageSize,
            SortBy        = sortBy,
            SortDirection = sortDirection
        }, ct);
        return OkResult(result);
    }

    [HttpGet("promotions/{id:guid}")]
    [HasPermission(Permissions.Promotions.Read)]
    [ProducesResponseType(typeof(ApiResponse<PromotionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPromotion(Guid id, CancellationToken ct)
        => OkResult(await Sender.Send(new GetPromotionQuery(id), ct));

    [HttpGet("promotions/validate")]
    [HasPermission(Permissions.Promotions.Read)]
    [ProducesResponseType(typeof(ApiResponse<PromotionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidatePromotionCode(
        [FromQuery] Guid corporationId,
        [FromQuery] string code,
        CancellationToken ct)
        => OkResult(await Sender.Send(new ValidatePromotionCodeQuery(corporationId, code), ct));

    [HttpPost("promotions")]
    [HasPermission(Permissions.Promotions.Create)]
    [ProducesResponseType(typeof(ApiResponse<PromotionDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePromotion(
        [FromBody] CreatePromotionRequest req, CancellationToken ct)
    {
        var result = await Sender.Send(new CreatePromotionCommand(
            req.CorporationId, req.Code, req.Name, req.Value,
            req.IsPercentage, req.ValidFrom, req.ValidTo, req.MaxRedemptions), ct);
        return CreatedResult(result, $"/api/payments/promotions/{result.Id}");
    }

    [HttpPut("promotions/{id:guid}")]
    [HasPermission(Permissions.Promotions.Update)]
    [ProducesResponseType(typeof(ApiResponse<PromotionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePromotion(
        Guid id, [FromBody] UpdatePromotionRequest req, CancellationToken ct)
        => OkResult(await Sender.Send(new UpdatePromotionCommand(
            id, req.Code, req.Name, req.Value,
            req.IsPercentage, req.ValidFrom, req.ValidTo, req.MaxRedemptions), ct));

    [HttpPost("promotions/{id:guid}/activate")]
    [HasPermission(Permissions.Promotions.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActivatePromotion(Guid id, CancellationToken ct)
    {
        await Sender.Send(new ActivatePromotionCommand(id), ct);
        return NoContentResult("Promotion activated.");
    }

    [HttpPost("promotions/{id:guid}/deactivate")]
    [HasPermission(Permissions.Promotions.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivatePromotion(Guid id, CancellationToken ct)
    {
        await Sender.Send(new DeactivatePromotionCommand(id), ct);
        return NoContentResult("Promotion deactivated.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FINANCE REPORTS
    // ══════════════════════════════════════════════════════════════════════════

    [HttpGet("reports/revenue")]
    [HasPermission(Permissions.FinanceReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<RevenueReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueReport(
        [FromQuery] Guid corporationId,
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd,
        CancellationToken ct)
        => OkResult(await Sender.Send(new GetRevenueReportQuery(
            corporationId, periodStart, periodEnd), ct));

    [HttpGet("reports/packages")]
    [HasPermission(Permissions.FinanceReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<PackageReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPackageReport(
        [FromQuery] Guid corporationId,
        CancellationToken ct)
        => OkResult(await Sender.Send(new GetPackageReportQuery(corporationId), ct));

    [HttpGet("reports/credit-usage")]
    [HasPermission(Permissions.FinanceReports.Read)]
    [ProducesResponseType(typeof(ApiResponse<CreditUsageReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCreditUsageReport(
        [FromQuery] Guid corporationId,
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd,
        CancellationToken ct)
        => OkResult(await Sender.Send(new GetCreditUsageReportQuery(
            corporationId, periodStart, periodEnd), ct));
}

// ── Request Records ───────────────────────────────────────────────────────────

// Package Definitions
public record CreatePackageDefinitionRequest(
    Guid CorporationId,
    string Code,
    string Name,
    decimal ListPrice,
    Guid? PackageTypeId,
    Guid? ProgramId,
    decimal? TotalCredits,
    int? ValidityDays,
    string Currency = "TRY");

public record UpdatePackageDefinitionRequest(
    string Code,
    string Name,
    decimal ListPrice,
    Guid? PackageTypeId,
    Guid? ProgramId,
    decimal? TotalCredits,
    int? ValidityDays,
    string Currency,
    int RowVersion);

// Student Packages
public record PurchasePackageRequest(
    Guid CorporationId,
    Guid StudentId,
    decimal TotalCredits,
    decimal Price,
    Guid? PackageDefinitionId,
    DateOnly? ExpiresOn,
    string Currency = "TRY");

public record CancelPackageRequest(int RowVersion);

// Credit Ledger
public record ConsumeCreditsRequest(
    Guid StudentPackageId,
    decimal Amount,
    Guid? SessionId,
    string? Reason);

public record GrantCreditsRequest(
    Guid StudentPackageId,
    decimal Amount,
    string Reason);

public record RefundCreditsRequest(
    Guid StudentPackageId,
    decimal Amount,
    string Reason);

public record AdjustCreditsRequest(
    Guid StudentPackageId,
    decimal Delta,
    string Reason);

// Invoices
public record CreateInvoiceRequest(
    Guid CorporationId,
    DateOnly IssueDate,
    string Currency = "TRY",
    Guid? StudentId = null,
    Guid? GuardianId = null,
    DateOnly? DueDate = null);

public record AddInvoiceLineRequest(
    string Description,
    decimal UnitPrice,
    decimal Quantity = 1,
    Guid? StudentPackageId = null,
    int SortOrder = 0);

public record IssueInvoiceRequest(string? InvoiceNo, int RowVersion);
public record VoidInvoiceRequest(int RowVersion);

// Payments
public record RecordPaymentRequest(
    Guid CorporationId,
    decimal Amount,
    string Currency = "TRY",
    Guid? InvoiceId = null,
    Guid? StudentId = null,
    Guid? PaymentMethodId = null,
    Guid? GatewayProviderId = null,
    string? GatewayReference = null,
    string? IdempotencyKey = null);

public record CapturePaymentRequest(
    string? GatewayReference,
    DateTimeOffset? PaidAt,
    int RowVersion);

// Refunds
public record RequestRefundRequest(
    Guid PaymentId,
    decimal Amount,
    string? Reason);

// Discounts
public record ApplyDiscountRequest(
    Guid CorporationId,
    decimal Value,
    bool IsPercentage = true,
    Guid? InvoiceId = null,
    Guid? StudentPackageId = null,
    Guid? DiscountTypeId = null,
    string? Reason = null);

// Scholarships
public record GrantScholarshipRequest(
    Guid CorporationId,
    Guid StudentId,
    Guid? ScholarshipTypeId,
    decimal? Percentage,
    decimal? Amount,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    string? Note);

public record UpdateScholarshipRequest(
    Guid? ScholarshipTypeId,
    decimal? Percentage,
    decimal? Amount,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    string? Note,
    int RowVersion);

// Promotions
public record CreatePromotionRequest(
    Guid CorporationId,
    string Code,
    string Name,
    decimal Value,
    bool IsPercentage = true,
    DateOnly? ValidFrom = null,
    DateOnly? ValidTo = null,
    int? MaxRedemptions = null);

public record UpdatePromotionRequest(
    string Code,
    string Name,
    decimal Value,
    bool IsPercentage,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int? MaxRedemptions);

// Shared
public record PaymentRowVersionRequest(int RowVersion);
