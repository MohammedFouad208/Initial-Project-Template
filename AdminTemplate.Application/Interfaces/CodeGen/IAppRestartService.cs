namespace AdminTemplate.Application.Interfaces.CodeGen;

public interface IAppRestartService
{
    Task BuildAndRestartAsync(string projectRootPath);
}
