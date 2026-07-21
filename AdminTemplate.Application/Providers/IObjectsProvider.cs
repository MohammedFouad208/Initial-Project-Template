using AdminTemplate.Application.DTOs;

namespace AdminTemplate.Application.Providers;

public interface IObjectsProvider
{
    IReadOnlyList<SidebarObjectDto> GetAll();
}
