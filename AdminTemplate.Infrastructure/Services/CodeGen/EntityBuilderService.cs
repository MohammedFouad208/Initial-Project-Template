using AdminTemplate.Application.DTOs.CodeGen;
using AdminTemplate.Application.Interfaces.CodeGen;
using AdminTemplate.Domain.Entities.CodeGen;
using AdminTemplate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AdminTemplate.Infrastructure.Services.CodeGen;

public class EntityBuilderService : IEntityBuilderService
{
    private readonly ApplicationDbContext _db;

    public EntityBuilderService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EntityDefinitionDto>> GetAllAsync()
    {
        var entities = await _db.EntityDefinitions
            .Include(e => e.Columns)
            .Include(e => e.Relations)
            .OrderBy(e => e.Name)
            .ToListAsync();

        return entities.Select(Map).ToList();
    }

    public async Task<EntityDefinitionDto?> GetByIdAsync(Guid id)
    {
        var entity = await _db.EntityDefinitions
            .Include(e => e.Columns)
            .Include(e => e.Relations)
            .FirstOrDefaultAsync(e => e.Id == id);

        return entity is null ? null : Map(entity);
    }

    public async Task<EntityDefinitionDto> CreateEntityAsync(CreateEntityDto dto)
    {
        var entity = new EntityDefinition
        {
            Name        = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            TableName   = string.IsNullOrWhiteSpace(dto.TableName) ? Pluralize(dto.Name) : dto.TableName.Trim(),
            CreatedAt   = DateTime.UtcNow
        };

        int order = 0;
        foreach (var col in dto.Columns)
        {
            entity.Columns.Add(new EntityColumn
            {
                Name                 = col.Name.Trim(),
                DataType             = col.DataType,
                IsRequired           = col.IsRequired,
                IsUnique             = col.IsUnique,
                MaxLength            = col.MaxLength,
                SortOrder            = col.SortOrder > 0 ? col.SortOrder : order++,
                DefaultValue         = col.DefaultValue?.Trim(),
                ShowInList           = col.ShowInList,
                ShowInForm           = col.ShowInForm,
                UseInSearch          = col.UseInSearch,
                CreatedAt            = DateTime.UtcNow
            });
        }

        foreach (var rel in dto.Relations)
        {
            entity.Relations.Add(new EntityRelation
            {
                RelatedEntityName      = rel.RelatedEntityName.Trim(),
                ForeignKeyName         = rel.ForeignKeyName.Trim(),
                NavigationPropertyName = rel.NavigationPropertyName.Trim(),
                DisplayColumn          = rel.DisplayColumn.Trim(),
                RelationType           = (RelationType)rel.RelationType,
                CreatedAt              = DateTime.UtcNow
            });
        }

        _db.EntityDefinitions.Add(entity);
        await _db.SaveChangesAsync();

        return Map(entity);
    }

    public async Task<EntityDefinitionDto?> UpdateEntityAsync(Guid id, CreateEntityDto dto)
    {
        var entity = await _db.EntityDefinitions
            .Include(e => e.Columns)
            .Include(e => e.Relations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null || entity.IsGenerated) return null;

        entity.Name        = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        entity.TableName   = string.IsNullOrWhiteSpace(dto.TableName) ? Pluralize(dto.Name) : dto.TableName.Trim();

        // Replace columns — snapshot first, then clear, then remove
        var oldColumns  = entity.Columns.ToList();
        entity.Columns.Clear();
        _db.Set<EntityColumn>().RemoveRange(oldColumns);

        int order = 0;
        foreach (var col in dto.Columns)
        {
            entity.Columns.Add(new EntityColumn
            {
                Name          = col.Name.Trim(),
                DataType      = col.DataType,
                IsRequired    = col.IsRequired,
                IsUnique      = col.IsUnique,
                MaxLength     = col.MaxLength,
                SortOrder     = col.SortOrder > 0 ? col.SortOrder : order++,
                DefaultValue  = col.DefaultValue?.Trim(),
                ShowInList    = col.ShowInList,
                ShowInForm    = col.ShowInForm,
                UseInSearch   = col.UseInSearch,
                CreatedAt     = DateTime.UtcNow
            });
        }

        // Replace relations — snapshot first, then clear, then remove
        var oldRelations = entity.Relations.ToList();
        entity.Relations.Clear();
        _db.Set<EntityRelation>().RemoveRange(oldRelations);

        foreach (var rel in dto.Relations)
        {
            entity.Relations.Add(new EntityRelation
            {
                RelatedEntityName      = rel.RelatedEntityName.Trim(),
                ForeignKeyName         = rel.ForeignKeyName.Trim(),
                NavigationPropertyName = rel.NavigationPropertyName.Trim(),
                DisplayColumn          = rel.DisplayColumn.Trim(),
                RelationType           = (RelationType)rel.RelationType,
                CreatedAt              = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task DeleteEntityAsync(Guid id, string projectRoot)
    {
        var entity = await _db.EntityDefinitions.FindAsync(id);
        if (entity is null) return;

        // The table itself is dropped by an EF migration generated from the removed
        // entity/DbSet (see EntityBuilderController.Delete). Here we only remove the
        // entity definition metadata.
        _db.EntityDefinitions.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task MarkGeneratedAsync(Guid id)
    {
        var entity = await _db.EntityDefinitions.FindAsync(id);
        if (entity is not null)
        {
            entity.IsGenerated = true;
            entity.GeneratedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EntityDefinitionDto Map(EntityDefinition e) => new(
        e.Id,
        e.Name,
        e.Description,
        e.TableName,
        e.IsGenerated,
        e.GeneratedAt,
        e.Columns.OrderBy(c => c.SortOrder).Select(c => new EntityColumnDto(
            c.Id, c.Name, c.DataType, c.IsRequired, c.IsUnique,
            c.MaxLength, c.SortOrder, c.DefaultValue, c.ShowInList, c.ShowInForm, c.UseInSearch)).ToList(),
        e.Relations.Select(r => new EntityRelationDto(
            r.Id, r.RelatedEntityName, r.ForeignKeyName, r.NavigationPropertyName,
            r.DisplayColumn, (int)r.RelationType)).ToList());

    private static string Pluralize(string name)
    {
        if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase) && name.Length > 1)
            return name[..^1] + "ies";
        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            return name + "es";
        return name + "s";
    }
}
