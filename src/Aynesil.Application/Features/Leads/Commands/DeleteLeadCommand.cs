using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Commands;

// ── Request ───────────────────────────────────────────────────────────────────
public record DeleteLeadCommand(Guid Id) : IRequest;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class DeleteLeadCommandHandler : IRequestHandler<DeleteLeadCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteLeadCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteLeadCommand req, CancellationToken ct)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == req.Id, ct)
            ?? throw new NotFoundException("Lead", req.Id);

        if (lead.ConvertedStudentId.HasValue)
            throw new InvalidOperationException(
                "Cannot delete a lead that has been converted to a student.");

        lead.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
