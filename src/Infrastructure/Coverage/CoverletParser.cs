using System.Globalization;
using System.Xml.Linq;

using Core.TestModels;

namespace Infrastructure.Coverage;

public static class CoverletParser
{
    public static TestToCodeMap Parse(string xmlPath)
    {
        var map = new TestToCodeMap { LastUpdated = DateTime.Now };
        var doc = XDocument.Load(xmlPath);

        var methods = doc.Descendants("method");

        foreach (var method in methods)
        {
            var methodName = method.Attribute("name")?.Value;

            var classElement = method.Ancestors("class").FirstOrDefault();
            var className = classElement?.Attribute("name")?.Value;

            if (methodName != null && className != null)
            {
                var hasHits = method
                    .Descendants("line")
                    .Any(l => int.Parse(l.Attribute("hits")?.Value ?? "0", CultureInfo.InvariantCulture) > 0);

                if (hasHits)
                {
                    var fullName = $"{className}.{methodName}";

                    if (!map.Map.TryGetValue("AllTests", out List<string>? value))
                    {
                        value = [];
                        map.Map["AllTests"] = value;
                    }

                    value.Add(fullName);
                }
            }
        }

        return map;
    }
}