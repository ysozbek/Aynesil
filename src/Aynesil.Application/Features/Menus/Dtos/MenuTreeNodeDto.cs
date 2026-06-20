namespace Aynesil.Application.Features.Menus.Dtos;

/// <summary>
/// Recursive node for the user-facing navigation tree.
/// Permission-filtered and locale-resolved before being returned to the client.
/// Children are sorted by SortOrder and carry their own subtrees.
/// </summary>
public record MenuTreeNodeDto(
    Guid Id,
    string Code,
    string Label,
    string? Route,
    string? Icon,
    int SortOrder,
    IReadOnlyList<MenuTreeNodeDto> Children);
