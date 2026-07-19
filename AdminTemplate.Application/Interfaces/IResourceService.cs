namespace AdminTemplate.Application.Interfaces;

public interface IResourceService
{
    IReadOnlyList<ResourceEntry> GetAll();
    void Upsert(string key, string value);
    void Delete(string key);
}

public record ResourceEntry(string Key, string Value);
