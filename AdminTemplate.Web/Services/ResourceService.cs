using System.Xml.Linq;
using AdminTemplate.Application.Interfaces;

namespace AdminTemplate.Web.Services;

public class ResourceService : IResourceService
{
    private readonly string _resxPath;
    private static readonly object _lock = new();

    public ResourceService(IWebHostEnvironment env)
    {
        _resxPath = Path.Combine(env.ContentRootPath, "Resources", "SharedResource.resx");
    }

    public IReadOnlyList<ResourceEntry> GetAll()
    {
        lock (_lock)
        {
            var doc = XDocument.Load(_resxPath);
            return doc.Root!
                .Elements("data")
                .Select(e => new ResourceEntry(
                    e.Attribute("name")!.Value,
                    e.Element("value")?.Value ?? string.Empty))
                .OrderBy(e => e.Key)
                .ToList();
        }
    }

    public void Upsert(string key, string value)
    {
        lock (_lock)
        {
            var doc = XDocument.Load(_resxPath);
            var root = doc.Root!;

            var existing = root.Elements("data")
                .FirstOrDefault(e => e.Attribute("name")?.Value == key);

            if (existing is not null)
            {
                existing.Element("value")!.Value = value;
            }
            else
            {
                root.Add(new XElement("data",
                    new XAttribute("name", key),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", value)));
            }

            doc.Save(_resxPath);
        }
    }

    public void Delete(string key)
    {
        lock (_lock)
        {
            var doc = XDocument.Load(_resxPath);
            doc.Root!
                .Elements("data")
                .FirstOrDefault(e => e.Attribute("name")?.Value == key)
                ?.Remove();
            doc.Save(_resxPath);
        }
    }
}
