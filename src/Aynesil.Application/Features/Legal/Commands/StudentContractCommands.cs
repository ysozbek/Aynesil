using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Legal.Dtos;
using Aynesil.Domain.Modules.Legal.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Legal.Commands;

// ── GenerateStudentContractCommand ────────────────────────────────────────────

public record GenerateStudentContractCommand(
    Guid StudentId,
    Guid? TemplateId,
    Guid? GuardianId,
    DateOnly? StartsOn,
    DateOnly? EndsOn) : IRequest<StudentContractDto>;

public class GenerateStudentContractCommandValidator
    : AbstractValidator<GenerateStudentContractCommand>
{
    public GenerateStudentContractCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x)
            .Must(x => !(x.StartsOn.HasValue && x.EndsOn.HasValue && x.EndsOn < x.StartsOn))
            .WithKey(MsgKeys.Validation.MustNotBeBefore, Msg.FieldArg("EndDate"), Msg.FieldArg("StartDate"));
    }
}

public sealed class GenerateStudentContractCommandHandler
    : IRequestHandler<GenerateStudentContractCommand, StudentContractDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public GenerateStudentContractCommandHandler(IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _db = db;
        _tenant = tenant;
    }

    public async Task<StudentContractDto> Handle(
        GenerateStudentContractCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var studentExists = await _db.Students
            .AnyAsync(s => s.CorporationId == corporationId && s.Id == req.StudentId && s.DeletedAt == null, ct);

        if (!studentExists)
            throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Student"));

        int? templateVersion = null;
        if (req.TemplateId.HasValue)
        {
            var tpl = await _db.ContractTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.CorporationId == corporationId && t.Id == req.TemplateId.Value, ct)
                ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "ContractTemplate"));

            if (!tpl.IsCurrent)
                throw new BusinessRuleViolationException(MsgKeys.Rule.ContractNeedsCurrentVersion);

            templateVersion = tpl.Version;
        }

        var contract = StudentContract.Generate(
            corporationId, req.StudentId,
            req.TemplateId, templateVersion,
            req.GuardianId, req.StartsOn, req.EndsOn, _currentUser.UserId);

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
    int RowVersion) : IRequest;

public class UpdateStudentContractCommandValidator
    : AbstractValidator<UpdateStudentContractCommand>
{
    public UpdateStudentContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => !(x.StartsOn.HasValue && x.EndsOn.HasValue && x.EndsOn < x.StartsOn))
            .WithKey(MsgKeys.Validation.MustNotBeBefore, Msg.FieldArg("EndDate"), Msg.FieldArg("StartDate"));
    }
}

public sealed class UpdateStudentContractCommandHandler : IRequestHandler<UpdateStudentContractCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateStudentContractCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "StudentContract"));

        if (contract.RowVersion != req.RowVersion)
            throw new BusinessRuleViolationException(MsgKeys.Error.Concurrency);

        if (req.GuardianId.HasValue && req.GuardianId != contract.GuardianId)
        {
            // Yeni veli, sözleşmenin öğrencisine bağlı olmalı (başka öğrencinin velisi atanamaz).
            var linked = await _db.StudentGuardians.AsNoTracking().AnyAsync(
                g => g.StudentId == contract.StudentId && g.GuardianId == req.GuardianId.Value, ct);
            if (!linked)
                throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Guardian"));
        }

        contract.UpdateDetails(req.GuardianId, req.StartsOn, req.EndsOn, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── SendStudentContractCommand ────────────────────────────────────────────────

public record SendStudentContractCommand(Guid Id) : IRequest;

public sealed class SendStudentContractCommandHandler : IRequestHandler<SendStudentContractCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SendStudentContractCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(SendStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "StudentContract"));

        contract.Send(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── SignStudentContractCommand ────────────────────────────────────────────────

public record SignStudentContractCommand(
    Guid Id,
    string SignedByName,
    string SignatureMethod,
    string? SignatureRef,
    Guid? SignedFileId) : IRequest;

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
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("SignatureMethod"), EnumSets.SignatureMethod);
    }
}

public sealed class SignStudentContractCommandHandler : IRequestHandler<SignStudentContractCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SignStudentContractCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(SignStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "StudentContract"));

        contract.Sign(req.SignedByName, req.SignatureMethod, req.SignatureRef, req.SignedFileId, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── ActivateStudentContractCommand ────────────────────────────────────────────

public record ActivateStudentContractCommand(
    Guid Id,
    Guid? SignedFileId = null) : IRequest;

public sealed class ActivateStudentContractCommandHandler : IRequestHandler<ActivateStudentContractCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ActivateStudentContractCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ActivateStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "StudentContract"));

        contract.Activate(req.SignedFileId, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── ExpireStudentContractCommand ──────────────────────────────────────────────

public record ExpireStudentContractCommand(Guid Id) : IRequest;

public sealed class ExpireStudentContractCommandHandler : IRequestHandler<ExpireStudentContractCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ExpireStudentContractCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ExpireStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "StudentContract"));

        contract.Expire(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── TerminateStudentContractCommand ───────────────────────────────────────────

public record TerminateStudentContractCommand(Guid Id) : IRequest;

public sealed class TerminateStudentContractCommandHandler : IRequestHandler<TerminateStudentContractCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TerminateStudentContractCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(TerminateStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "StudentContract"));

        contract.Terminate(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteStudentContractCommand ──────────────────────────────────────────────

public record DeleteStudentContractCommand(Guid Id) : IRequest;

public sealed class DeleteStudentContractCommandHandler : IRequestHandler<DeleteStudentContractCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteStudentContractCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteStudentContractCommand req, CancellationToken ct)
    {
        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "StudentContract"));

        contract.Delete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
