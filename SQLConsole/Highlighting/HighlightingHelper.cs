using System.IO;
using System.Reflection;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;

namespace Recom.SQLConsole.Highlighting;

public static class HighlightingHelper
{
    public static void RegisterHighlighting(string name, string extension, string resourceName)
    {
        string? fullResource = Assembly.GetExecutingAssembly().GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(resourceName), string.Empty);

        IHighlightingDefinition customHighlighting;
        using (Stream? s = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullResource))
        {
            if (s == null)
            {
                throw new InvalidOperationException("Could not find embedded resource");
            }

            using (XmlReader reader = new XmlTextReader(s))
            {
                customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }

        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        HighlightingManager.Instance.RegisterHighlighting(name, [extension], customHighlighting);
    }
}