namespace AdminTemplate.Application.Interfaces.CodeGen;

public interface IEntityPermissionSeeder
{
    Task SeedPermissionsAsync(string entityName);
}
