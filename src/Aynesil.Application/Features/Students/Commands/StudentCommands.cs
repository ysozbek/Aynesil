using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── CreateStudentCommand ──────────────────────────────────────────────────────

public record CreateStudentCommand(
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
    string? Notes) : IRequest<StudentDto>;

public class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StudentNo).MaximumLength(50).When(x => x.StudentNo is not null);
    }
}

public sealed class CreateStudentCommandHandler
    : IRequestHandler<CreateStudentCommand, StudentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateStudentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StudentDto> Handle(CreateStudentCommand req, CancellationToken ct)
    {
        if (req.StudentNo is not null)
        {
            var taken = await _db.Students
                .AnyAsync(s => s.CorporationId == req.CorporationId && s.StudentNo == req.StudentNo, ct);
            if (taken)
                throw new InvalidOperationException(
                    $"Student number '{req.StudentNo}' is already in use within this corporation.");
        }

        if (req.StatusId.HasValue)
        {
            var validStatus = await _db.RefValues.AnyAsync(
                r => r.Id == req.StatusId.Value && r.RefType!.Code == "student_status", ct);
            if (!validStatus)
                throw new KeyNotFoundException($"Invalid student_status ref_value: {req.StatusId}");
        }

        var student = Student.Create(
            req.CorporationId, req.FirstName, req.LastName,
            req.StudentNo, req.NationalId, req.BirthDate, req.Gender,
            req.PrimaryCampusId, req.StatusId, req.LeadId, req.Notes,
            _currentUser.UserId);

        _db.Students.Add(student);
        await _db.SaveChangesAsync(ct);

        return (await StudentProjection.LoadStudentAsync(_db, student.Id, ct))!;
    }
}

// ── UpdateStudentCommand ──────────────────────────────────────────────────────

public record UpdateStudentCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? StudentNo,
    string? NationalId,
    DateOnly? BirthDate,
    string? Gender,
    Guid? PrimaryCampusId,
    string? Notes,
    int RowVersion) : IRequest<StudentDto>;

public class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
{
    public UpdateStudentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StudentNo).MaximumLength(50).When(x => x.StudentNo is not null);
    }
}

public sealed class UpdateStudentCommandHandler
    : IRequestHandler<UpdateStudentCommand, StudentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateStudentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StudentDto> Handle(UpdateStudentCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student {req.Id} not found.");

        if (student.RowVersion != req.RowVersion)
            throw new InvalidOperationException("The student record was modified by another user. Please refresh and retry.");

        if (req.StudentNo is not null)
        {
            var taken = await _db.Students.AnyAsync(
                s => s.CorporationId == student.CorporationId
                  && s.StudentNo == req.StudentNo
                  && s.Id != req.Id, ct);
            if (taken)
                throw new InvalidOperationException(
                    $"Student number '{req.StudentNo}' is already in use within this corporation.");
        }

        student.UpdateProfile(
            req.FirstName, req.LastName, req.StudentNo,
            req.NationalId, req.BirthDate, req.Gender,
            req.PrimaryCampusId, req.Notes, _currentUser.UserId);

        await _db.SaveChangesAsync(ct);

        return (await StudentProjection.LoadStudentAsync(_db, student.Id, ct))!;
    }
}

// ── DeleteStudentCommand ──────────────────────────────────────────────────────

public record DeleteStudentCommand(Guid Id) : IRequest;

public sealed class DeleteStudentCommandHandler : IRequestHandler<DeleteStudentCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteStudentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteStudentCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student {req.Id} not found.");

        student.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
