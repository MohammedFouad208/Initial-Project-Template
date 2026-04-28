using AdminTemplate.Application.DTOs.CodeGen;

namespace AdminTemplate.Application.Interfaces.CodeGen;

public record GenerationResult(bool Success, string? Error, IReadOnlyList<string> GeneratedFiles);

public interface ICodeGeneratorService
{
    Task<GenerationResult> GenerateAsync(EntityDefinitionDto entity, string projectRootPath);
    Task DeleteGeneratedAsync(EntityDefinitionDto entity, string projectRootPath);
}
