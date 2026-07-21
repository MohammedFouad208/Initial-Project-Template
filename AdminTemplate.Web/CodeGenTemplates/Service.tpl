using AdminTemplate.Application.Common.DataTable;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Domain.Entities;
using AdminTemplate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AdminTemplate.Infrastructure.Services;

/// <summary>
/// Auto-generated service implementation for: {{ entity.name }}
/// Generated at: {{ generated_at }}
/// </summary>
public class {{ entity.name }}Service : I{{ entity.name }}Service
{
    private readonly ApplicationDbContext _db;

    public {{ entity.name }}Service(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<object> Items, int Total)> GetPagedAsync(
        DataTableRequest request,
        Dictionary<string, string> filters)
    {
        string[] columnMap =
        [
            {{- for col in entity.columns }}
            {{- if col.show_in_list }}
            "{{ col.name | string.slice 0 1 | string.downcase }}{{ col.name | string.slice 1 }}",
            {{- end }}
            {{- end }}
        ];

        var sortCol = request.SortColumn >= 0 && request.SortColumn < columnMap.Length
            ? columnMap[request.SortColumn]
            : columnMap.Length > 0 ? columnMap[0] : "id";

        var pageIndex = request.Length > 0 ? request.Start / request.Length : 0;

        var query = _db.Set<{{ entity.name }}>()
            {{- for rel in entity.relations }}
            .Include(x => x.{{ rel.navigation_property_name }})
            {{- end }}
            .AsQueryable();

        // ── Search ────────────────────────────────────────────────────────────
        {{- for col in entity.columns }}
        {{- if col.use_in_search }}
        if (filters.TryGetValue("{{ col.name | string.slice 0 1 | string.downcase }}{{ col.name | string.slice 1 }}", out var filter{{ col.name }}) && !string.IsNullOrWhiteSpace(filter{{ col.name }}))
        {
            var term{{ col.name }} = filter{{ col.name }}.Trim().ToLower();
            query = query.Where(x => x.{{ col.name }}.ToLower().Contains(term{{ col.name }}));
        }
        {{- end }}
        {{- end }}
        {{- for rel in entity.relations }}
        if (filters.TryGetValue("rel_{{ rel.navigation_property_name | string.slice 0 1 | string.downcase }}{{ rel.navigation_property_name | string.slice 1 }}", out var filterRel{{ rel.navigation_property_name }}) && !string.IsNullOrWhiteSpace(filterRel{{ rel.navigation_property_name }}))
        {
            if (Guid.TryParse(filterRel{{ rel.navigation_property_name }}.Trim(), out var relId{{ rel.navigation_property_name }}))
                query = query.Where(x => x.{{ rel.foreign_key_name }} == relId{{ rel.navigation_property_name }});
        }
        {{- end }}

        var total = await query.CountAsync();

        // ── Sort ──────────────────────────────────────────────────────────────
        query = sortCol switch
        {
            {{- for col in entity.columns }}
            {{- if col.show_in_list }}
            "{{ col.name | string.slice 0 1 | string.downcase }}{{ col.name | string.slice 1 }}" =>
                request.SortDirection == "desc"
                    ? query.OrderByDescending(x => x.{{ col.name }})
                    : query.OrderBy(x => x.{{ col.name }}),
            {{- end }}
            {{- end }}
            _ => query.OrderBy(x => x.CreatedAt)
        };

        var items = await query
            .Skip(pageIndex * request.Length)
            .Take(request.Length)
            .Select(x => new
            {
                x.Id,
                {{- for col in entity.columns }}
                {{- if col.show_in_list }}
                x.{{ col.name }},
                {{- end }}
                {{- end }}
                {{- for rel in entity.relations }}
                {{ rel.navigation_property_name }}{{ rel.display_column }} = x.{{ rel.navigation_property_name }} != null
                    ? x.{{ rel.navigation_property_name }}.{{ rel.display_column }}
                    : null,
                {{- end }}
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task<{{ entity.name }}?> GetByIdAsync(Guid id)
    {
        return await _db.Set<{{ entity.name }}>()
            {{- for rel in entity.relations }}
            .Include(x => x.{{ rel.navigation_property_name }})
            {{- end }}
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task CreateAsync({{ entity.name }} model)
    {
        model.Id        = Guid.NewGuid();
        model.CreatedAt = DateTime.UtcNow;
        _db.Set<{{ entity.name }}>().Add(model);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(Guid id, {{ entity.name }} model)
    {
        var existing = await _db.Set<{{ entity.name }}>().FindAsync(id);
        if (existing is null) return false;

        {{- for col in entity.columns }}
        existing.{{ col.name }} = model.{{ col.name }};
        {{- end }}
        {{- for rel in entity.relations }}
        existing.{{ rel.foreign_key_name }} = model.{{ rel.foreign_key_name }};
        {{- end }}

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await _db.Set<{{ entity.name }}>().FindAsync(id);
        if (item is null) return false;

        _db.Set<{{ entity.name }}>().Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }
}
