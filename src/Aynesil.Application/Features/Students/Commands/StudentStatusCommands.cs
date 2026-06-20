using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── ChangeStudentStatusCommand ────────────────────────────────────────────────

/// <summary>
/// Changes a student's lifecycle status. The new status must be a valid ref_value of
/// ref_type 'student_status'. Appends a StudentStatusHistory record.
/// </summary>
public record ChangeStudentStatusCommand(
    Guid StudentId,
    Guid NewStatusId,
    string? Reason,
    int RowVersion) : IRequest<StudentDto>;

public class ChangeStudentStatusCommandValidator : AbstractValidator<ChangeStudentStatusCommand>
{
    public ChangeStudentStatusCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.NewStatusId).NotEmpty();
    }
}

public sealed class ChangeStudentStatusCommandHandler
    : IRequestHandler<ChangeStudentStatusCommand, StudentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ChangeStudentStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StudentDto> Handle(ChangeStudentStatusCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .FirstOrDefaultAsync(s => s.Id == req.StudentId, ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        if (student.RowVersion != req.RowVersion)
            throw new InvalidOperationException("The student record was modified by another user. Please refresh and retry.");

        var validStatus = await _db.RefValues.AnyAsync(
            r => r.Id == req.NewStatusId && r.RefType!.Code == "student_status", ct);
        if (!validStatus)
            throw new KeyNotFoundException($"Invalid student_status ref_value: {req.NewStatusId}");

        var history = student.ChangeStatus(req.NewStatusId, req.Reason, _currentUser.UserId);

        _db.StudentStatusHistories.Add(history);
        await _db.SaveChangesAsync(ct);

        return (await StudentProjection.LoadStudentAsync(_db, student.Id, ct))!;
    }
}
