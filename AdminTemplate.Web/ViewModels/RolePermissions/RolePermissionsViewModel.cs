using AdminTemplate.Application.DTOs;

namespace AdminTemplate.Web.ViewModels.RolePermissions;

public class RolePermissionsViewModel
{
    public string RoleId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public IReadOnlyList<PermissionObjectDto> AllObjects { get; init; } = [];
    public HashSet<string> AssignedKeys { get; init; } = [];
}
