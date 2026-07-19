using AdminTemplate.Application.Common.DataTable;
using AdminTemplate.Domain.Entities;

namespace AdminTemplate.Application.Interfaces;

/// <summary>
/// Auto-generated service interface for: {{ entity.name }}
/// Generated at: {{ generated_at }}
/// </summary>
public interface I{{ entity.name }}Service
{
    Task<(IEnumerable<object> Items, int Total)> GetPagedAsync(
        DataTableRequest request,
        Dictionary<string, string> filters);

    Task<{{ entity.name }}?> GetByIdAsync(Guid id);

    Task CreateAsync({{ entity.name }} model);

    Task<bool> UpdateAsync(Guid id, {{ entity.name }} model);

    Task<bool> DeleteAsync(Guid id);
}
