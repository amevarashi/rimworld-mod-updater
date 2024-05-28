using System.Xml.Linq;

namespace RimWorldModUpdater.Models;

public class RimWorldModManifest(XElement about)
{
    public string Version { get; } = about.Element("version")?.Value ?? "<version> not found";
    public string Identifier { get; } = about.Element("identifier")?.Value ?? "<identifier> not found";

    public static RimWorldModManifest FromXml(string xml)
    {
        return new RimWorldModManifest(XElement.Parse(xml));
    }

    public override string ToString()
    {
        return $"RimWorldModManifest[{Identifier}]";
    }
}