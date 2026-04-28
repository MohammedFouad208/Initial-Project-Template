using AdminTemplate.Application.DTOs.CodeGen;

namespace AdminTemplate.Application.Interfaces.CodeGen;

public interface IEntityBuilderService
{
    Task<IReadOnlyList<EntityDefinitionDto>> GetAllAsync();
    Task<EntityDefinitionDto?> GetByIdAsync(Guid id);
    Task<EntityDefinitionDto> CreateEntityAsync(CreateEntityDto dto);
    Task<EntityDefinitionDto?> UpdateEntityAsync(Guid id, CreateEntityDto dto);
    Task DeleteEntityAsync(Guid id);
    Task MarkGeneratedAsync(Guid id);

    Task<string> GenerateSqlAsync(EntityDefinitionDto entity);
    Task ExecuteSqlAsync(string sql);
}
