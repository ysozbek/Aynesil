using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Legal.Dtos;
using Aynesil.Domain.Modules.Legal.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Legal.Commands;

// ── GenerateStudentContractCommand ────────────────────────────────────────────

public record GenerateStudentContractCommand(
    Guid CorporationId,
    Guid StudentId,
    Guid? TemplateId,
    Guid? GuardianId,
    DateOnly? StartsOn,
    DateOnly? EndsOn,
    Guid? CreatedBy = null) : IRequest<StudentContractDto>;

public class GenerateStudentContractCommandValidator
    : AbstractValidator<GenerateStudentContractCommand>
{
    public GenerateStudentContractCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x)
            .Must(x => !(x.StartsOn.HasValue && x.EndsOn.HasValue && x.EndsOn < x.StartsOn))
            .WithMessage("End date cannot be before start date.");
    }
}

public sealed class GenerateStudentContractCommandHandler
    : IRequestHandler<GenerateStudentContractCommand, StudentContractDto>
{
    private readonly IAppDbContext _db;

    public GenerateStudentContractCommandHandler(IAppDbContext db) => _db = db;

    public async Task<StudentContractDto> Handle(
        GenerateStudentContractCommand req, CancellationToken ct)
    {
        var studentExists = await _db.Students
            .AnyAsync(s => s.Id == req.StudentId && s.DeletedAt == null, ct);

        if (!studentExists)
            throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        int? templateVersion = null;
        if (req.TemplateId.HasValue)
        {
            var tpl = await _db.ContractTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == req.TemplateId.Value, ct)
                ?? throw new KeyNotFoundException($"Contract template {req.TemplateId} not found.");

            if (!tpl.IsCurrent)
                throw new InvalidOperationException(
                    "Contracts may only be generated from the current version of a template.");

            templateVersion = tpl.Version;
        }

        var contract = StudentContract.Generate(
            req.CorporationId, req.StudentId,
            req.TemplateId, templateVersion,
            req.GuardianId, req.StartsOn, req.EndsOn, req.CreatedBy);

        _db.StudentContracts.Add(contract);
        await _db.SaveChangesAsync(ct);

        return await BuildDtoAsync(contract.Id, ct);
    }

    internal async Task<StudentContractDto> BuildDtoAsync(Guid id, CancellationToken ct)
    {
        var c = await _db.StudentContracts.AsNoTracking()
            .FirstAsync(x => x.Id == id, ct);

        var studentName = await _db.Students.AsNoTracking()
            .Where(s => s.Id == c.StudentId)
            .Select(s => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync(ct);

        string? templateCode = null;
        if (c.TemplateId.HasValue)
            templateCode = await _db.ContractTemplates.AsNoTracking()
                .Where(t => t.Id == c.TemplateId.Value)
                .Select(t => t.Code)
                .FirstOrDefaultAsync(ct);

        return new StudentContractDto(
            c.Id, c.CorporationId, c.StudentId, studentName,
            c.TemplateId, templateCode, c.TemplateVersion,
            c.GuardianId, c.Status,
            c.SignedAt, c.SignedByName, c.SignatureMethod, c.SignatureRef, c.SignedFileId,
            c.StartsOn, c.EndsOn,
            c.CreatedAt, c.CreatedBy, c.UpdatedAt, c.RowVersion);
    }
}

// ── UpdateStudentContractCommand ──────────────────────────────────────────────

public record UpdateStudentContractCommand(
    Guid Id,
    Guid? GuardianId,
    DateOnly? StartsOn,
    DateOnly? EndsOn,
    int RowVersion,
    Guid? UpdatedBy = null) : IRequest;

public class UpdateStudentContractCommandValidator
    : AbstractValidator<UpdateStudentContractCommand>
{
    public UpdateStudentContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => !(x.StartsOn.HasValue && x.EndsOn.HasValue && x.EndsOn < x.StartsOn))
            .WithMessage("End date cannot be before start date.");
    }
}

public sealed class UpdateStudentContractCommandHandler : IRequestHandler<UpdateStudentContractCommand>
{
    private readonly IAppDbContext _db;

    public UpdateStudentContractCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student contract {req.Id} not found.");

        contract.UpdateDetails(req.GuardianId, req.StartsOn, req.EndsOn, req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── SendStudentContractCommand ────────────────────────────────────────────────

public record SendStudentContractCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class SendStudentContractCommandHandler : IRequestHandler<SendStudentContractCommand>
{
    private readonly IAppDbContext _db;

    public SendStudentContractCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SendStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student contract {req.Id} not found.");

        contract.Send(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── SignStudentContractCommand ────────────────────────────────────────────────

public record SignStudentContractCommand(
    Guid Id,
    string SignedByName,
    string SignatureMethod,
    string? SignatureRef,
    Guid? SignedFileId,
    Guid? UpdatedBy = null) : IRequest;

public class SignStudentContractCommandValidator : AbstractValidator<SignStudentContractCommand>
{
    private static readonly string[] ValidMethods = ["wet", "e_sign", "click_wrap"];

    public SignStudentContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SignedByName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SignatureMethod)
            .NotEmpty()
            .Must(m => ValidMethods.Contains(m))
            .WithMessage("signatureMethod must be one of: wet, e_sign, click_wrap.");
    }
}

public sealed class SignStudentContractCommandHandler : IRequestHandler<SignStudentContractCommand>
{
    private readonly IAppDbContext _db;

    public SignStudentContractCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SignStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student contract {req.Id} not found.");

        contract.Sign(req.SignedByName, req.SignatureMethod, req.SignatureRef, req.SignedFileId, req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── ActivateStudentContractCommand ────────────────────────────────────────────

public record ActivateStudentContractCommand(
    Guid Id,
    Guid? SignedFileId = null,
    Guid? UpdatedBy = null) : IRequest;

public sealed class ActivateStudentContractCommandHandler : IRequestHandler<ActivateStudentContractCommand>
{
    private readonly IAppDbContext _db;

    public ActivateStudentContractCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ActivateStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student contract {req.Id} not found.");

        contract.Activate(req.SignedFileId, req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── ExpireStudentContractCommand ──────────────────────────────────────────────

public record ExpireStudentContractCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class ExpireStudentContractCommandHandler : IRequestHandler<ExpireStudentContractCommand>
{
    private readonly IAppDbContext _db;

    public ExpireStudentContractCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ExpireStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student contract {req.Id} not found.");

        contract.Expire(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── TerminateStudentContractCommand ───────────────────────────────────────────

public record TerminateStudentContractCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class TerminateStudentContractCommandHandler : IRequestHandler<TerminateStudentContractCommand>
{
    private readonly IAppDbContext _db;

    public TerminateStudentContractCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(TerminateStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student contract {req.Id} not found.");

        contract.Terminate(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteStudentContractCommand ──────────────────────────────────────────────

public record DeleteStudentContractCommand(Guid Id, Guid? DeletedBy = null) : IRequest;

public sealed class DeleteStudentContractCommandHandler : IRequestHandler<DeleteStudentContractCommand>
{
    private readonly IAppDbContext _db;

    public DeleteStudentContractCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student contract {req.Id} not found.");

        contract.Delete(req.DeletedBy);
        await _db.SaveChangesAsync(ct);
    }
}
