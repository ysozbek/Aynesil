using Aynesil.Application.Common.Identity;

namespace Aynesil.Api.Controllers;

// Request bodies shared by the ".../{id}/user-account" endpoints (EducatorsController, GuardiansController).

public record LinkUserAccountRequest(Guid UserId);

public record CreateLinkedUserAccountRequest(
    string Username,
    string FullName,
    string? Email,
    string? Phone,
    Guid RoleId,
    Guid? CampusId)
{
    public NewLinkedUserAccount ToModel() => new(Username, FullName, Email, Phone, RoleId, CampusId);
}
