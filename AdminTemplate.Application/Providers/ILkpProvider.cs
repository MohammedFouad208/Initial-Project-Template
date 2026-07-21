using AdminTemplate.Application.DTOs;

namespace AdminTemplate.Application.Providers;

public interface ILkpProvider
{
    IReadOnlyList<LkpTableDto> GetAll();
    void AddTable(string name, string displayName);
    void RemoveTable(string name);
    void Reload();
}
