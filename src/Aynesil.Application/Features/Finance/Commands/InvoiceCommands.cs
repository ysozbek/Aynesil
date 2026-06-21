using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using Aynesil.Domain.Modules.Finance.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Commands;

// ── CreateInvoiceCommand ──────────────────────────────────────────────────────

public record CreateInvoiceCommand(
    Guid CorporationId,
    DateOnly IssueDate,
    string Currency = "TRY",
    Guid? StudentId = null,
    Guid? GuardianId = null,
    DateOnly? DueDate = null) : IRequest<InvoiceDto>;

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.IssueDate)
            .When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be on or after issue date.");
    }
}

public sealed class CreateInvoiceCommandHandler
    : IRequestHandler<CreateInvoiceCommand, InvoiceDto>
{
    private readonly IAppDbContext _db;

    public CreateInvoiceCommandHandler(IAppDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(CreateInvoiceCommand req, CancellationToken ct)
    {
        var invoice = Invoice.Create(
            req.CorporationId, req.IssueDate, req.Currency,
            req.StudentId, req.GuardianId, req.DueDate);

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        return (await FinanceProjection.LoadInvoiceAsync(_db, invoice.Id, ct))!;
    }
}

// ── AddInvoiceLineCommand ─────────────────────────────────────────────────────

public record AddInvoiceLineCommand(
    Guid InvoiceId,
    string Description,
    decimal UnitPrice,
    decimal Quantity = 1,
    Guid? StudentPackageId = null,
    int SortOrder = 0) : IRequest<InvoiceDto>;

public class AddInvoiceLineCommandValidator : AbstractValidator<AddInvoiceLineCommand>
{
    public AddInvoiceLineCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class AddInvoiceLineCommandHandler
    : IRequestHandler<AddInvoiceLineCommand, InvoiceDto>
{
    private readonly IAppDbContext _db;

    public AddInvoiceLineCommandHandler(IAppDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(AddInvoiceLineCommand req, CancellationToken ct)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Discounts)
            .FirstOrDefaultAsync(i => i.Id == req.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {req.InvoiceId} not found.");

        if (invoice.Status != "draft")
            throw new InvalidOperationException("Lines can only be added to draft invoices.");

        var line = InvoiceLine.Create(
            invoice.CorporationId, req.InvoiceId,
            req.Description, req.UnitPrice,
            req.Quantity, req.StudentPackageId, req.SortOrder);

        _db.InvoiceLines.Add(line);

        invoice.Lines.Add(line);
        invoice.RecalculateTotals();

        await _db.SaveChangesAsync(ct);

        return (await FinanceProjection.LoadInvoiceAsync(_db, req.InvoiceId, ct))!;
    }
}

// ── RemoveInvoiceLineCommand ──────────────────────────────────────────────────

public record RemoveInvoiceLineCommand(Guid InvoiceId, Guid LineId) : IRequest<InvoiceDto>;

public sealed class RemoveInvoiceLineCommandHandler
    : IRequestHandler<RemoveInvoiceLineCommand, InvoiceDto>
{
    private readonly IAppDbContext _db;

    public RemoveInvoiceLineCommandHandler(IAppDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(RemoveInvoiceLineCommand req, CancellationToken ct)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Discounts)
            .FirstOrDefaultAsync(i => i.Id == req.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {req.InvoiceId} not found.");

        if (invoice.Status != "draft")
            throw new InvalidOperationException("Lines can only be removed from draft invoices.");

        var line = invoice.Lines.FirstOrDefault(l => l.Id == req.LineId)
            ?? throw new KeyNotFoundException($"Invoice line {req.LineId} not found.");

        _db.InvoiceLines.Remove(line);
        invoice.Lines.Remove(line);
        invoice.RecalculateTotals();

        await _db.SaveChangesAsync(ct);

        return (await FinanceProjection.LoadInvoiceAsync(_db, req.InvoiceId, ct))!;
    }
}

// ── IssueInvoiceCommand ───────────────────────────────────────────────────────

public record IssueInvoiceCommand(Guid Id, string? InvoiceNo, int RowVersion)
    : IRequest<InvoiceDto>;

public sealed class IssueInvoiceCommandHandler
    : IRequestHandler<IssueInvoiceCommand, InvoiceDto>
{
    private readonly IAppDbContext _db;

    public IssueInvoiceCommandHandler(IAppDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(IssueInvoiceCommand req, CancellationToken ct)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Discounts)
            .FirstOrDefaultAsync(i => i.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Invoice {req.Id} not found.");

        if (invoice.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "Invoice was modified by another user. Please refresh and retry.");

        if (!invoice.Lines.Any())
            throw new InvalidOperationException("Cannot issue an invoice with no lines.");

        if (req.InvoiceNo is not null)
            invoice.AssignNumber(req.InvoiceNo);

        invoice.RecalculateTotals();
        invoice.Issue();

        await _db.SaveChangesAsync(ct);

        return (await FinanceProjection.LoadInvoiceAsync(_db, req.Id, ct))!;
    }
}

// ── VoidInvoiceCommand ────────────────────────────────────────────────────────

public record VoidInvoiceCommand(Guid Id, int RowVersion) : IRequest<InvoiceDto>;

public sealed class VoidInvoiceCommandHandler
    : IRequestHandler<VoidInvoiceCommand, InvoiceDto>
{
    private readonly IAppDbContext _db;

    public VoidInvoiceCommandHandler(IAppDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(VoidInvoiceCommand req, CancellationToken ct)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.Discounts)
            .FirstOrDefaultAsync(i => i.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Invoice {req.Id} not found.");

        if (invoice.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "Invoice was modified by another user. Please refresh and retry.");

        invoice.Void();
        await _db.SaveChangesAsync(ct);

        return (await FinanceProjection.LoadInvoiceAsync(_db, req.Id, ct))!;
    }
}
