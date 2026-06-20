using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Users.Dtos;
using MediatR;

namespace Aynesil.Application.Features.Users.Queries;

public record GetUserQuery(Guid UserId) : IRequest<UserDto>;

public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IAppDbContext _db;

    public GetUserQueryHandler(IAppDbContext db) => _db = db;

    public async Task<UserDto> Handle(GetUserQuery req, CancellationToken ct)
    {
        var user = await _db.UserAccounts.FindAsync([req.UserId], ct)
            ?? throw new NotFoundException("User", req.UserId);

        return user.ToDto();
    }
}
