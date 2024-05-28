using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RimWorldModUpdater.Models;

public class RimWorldModAbout(XElement about)
{
    public string Id { get; } = about.Element("packageId")?.Value ?? "<packageId> not found";
    public string Name { get; } = about.Element("name")?.Value ?? "<name> not found";
    public string Author { get; } = about.Element("author")?.Value ?? "<author> not found";
    public string? ModVersion { get; } = about.Element("modVersion")?.Value;
    public string Description { get; } = about.Element("description")?.Value ?? "<description> not found";
    public HashSet<string> SupportedVersions { get; } = about.Element("supportedVersions")?
        .Elements()
        .Select(x => x.Value)
        .ToHashSet() ?? [];

    public static RimWorldModAbout FromXml(string xml)
    {
        return new RimWorldModAbout(XElement.Parse(xml));
    }

    public override string ToString()
    {
        return $"RimWorldModAbout[{Id}]";
    }
}