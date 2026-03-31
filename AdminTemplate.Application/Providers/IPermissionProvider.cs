using AdminTemplate.Application.DTOs;

namespace AdminTemplate.Application.Providers;

public interface IPermissionProvider
{
    IReadOnlyList<PermissionObjectDto> GetAll();
}
