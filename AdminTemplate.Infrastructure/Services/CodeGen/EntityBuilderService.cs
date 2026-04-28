using AdminTemplate.Application.DTOs.CodeGen;
using AdminTemplate.Application.Interfaces.CodeGen;
using AdminTemplate.Domain.Entities.CodeGen;
using AdminTemplate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

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

    public async Task DeleteEntityAsync(Guid id)
    {
        var entity = await _db.EntityDefinitions.FindAsync(id);
        if (entity is null) return;

        // Drop the generated table if it exists
        var tableName = string.IsNullOrWhiteSpace(entity.TableName) ? Pluralize(entity.Name) : entity.TableName;
        await _db.Database.ExecuteSqlRawAsync(
            $"IF EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}') DROP TABLE [{tableName}]");

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

    public async Task<string> GenerateSqlAsync(EntityDefinitionDto entity)
    {
        // Build a lookup of entity name → actual table name for FK references
        var allEntities = await _db.EntityDefinitions
            .Select(e => new { e.Name, e.TableName })
            .ToListAsync();
        var tableNameLookup = allEntities.ToDictionary(
            e => e.Name,
            e => e.TableName ?? Pluralize(e.Name),
            StringComparer.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        var tableName = entity.TableName ?? Pluralize(entity.Name);

        sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}')");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"    CREATE TABLE [{tableName}] (");
        sb.AppendLine("        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,");
        sb.AppendLine("        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),");

        foreach (var col in entity.Columns.OrderBy(c => c.SortOrder))
        {
            var sqlType = MapToSqlType(col.DataType, col.MaxLength);
            var nullable = col.IsRequired ? "NOT NULL" : "NULL";
            var defaultClause = col.DefaultValue is not null ? $" DEFAULT {col.DefaultValue}" : string.Empty;
            sb.AppendLine($"        [{col.Name}] {sqlType} {nullable}{defaultClause},");
        }

        foreach (var rel in entity.Relations)
        {
            sb.AppendLine($"        [{rel.ForeignKeyName}] UNIQUEIDENTIFIER NOT NULL,");
        }

        // Remove trailing comma from last column line
        var sql = sb.ToString().TrimEnd();
        var lastComma = sql.LastIndexOf(',');
        if (lastComma >= 0)
        {
            sql = sql.Remove(lastComma, 1);
        }

        sb.Clear();
        sb.AppendLine(sql);
        sb.AppendLine("    );");

        // Unique indexes
        foreach (var col in entity.Columns.Where(c => c.IsUnique))
        {
            sb.AppendLine($"    CREATE UNIQUE INDEX [UX_{tableName}_{col.Name}] ON [{tableName}] ([{col.Name}]);");
        }

        // FK constraints — use the related entity's actual TableName, not a pluralized guess
        foreach (var rel in entity.Relations)
        {
            var relatedTable = tableNameLookup.TryGetValue(rel.RelatedEntityName, out var t)
                ? t
                : Pluralize(rel.RelatedEntityName);
            sb.AppendLine($"    ALTER TABLE [{tableName}] ADD CONSTRAINT [FK_{tableName}_{rel.ForeignKeyName}]");
            sb.AppendLine($"        FOREIGN KEY ([{rel.ForeignKeyName}]) REFERENCES [{relatedTable}]([Id]);");
        }

        sb.AppendLine("END");

        return sb.ToString();
    }

    public async Task ExecuteSqlAsync(string sql)
    {
        await _db.Database.ExecuteSqlRawAsync(sql);
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

    private static string MapToSqlType(string dataType, int? maxLength) => dataType.ToLowerInvariant() switch
    {
        "string"   => maxLength.HasValue ? $"NVARCHAR({maxLength})" : "NVARCHAR(MAX)",
        "int"      => "INT",
        "long"     => "BIGINT",
        "decimal"  => "DECIMAL(18,2)",
        "double"   => "FLOAT",
        "bool"     => "BIT",
        "datetime" => "DATETIME2",
        "guid"     => "UNIQUEIDENTIFIER",
        _          => "NVARCHAR(255)"
    };

    private static string Pluralize(string name)
    {
        if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase) && name.Length > 1)
            return name[..^1] + "ies";
        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            return name + "es";
        return name + "s";
    }
}
