using System.IO;

namespace Recom.SQLConsole.Properties;

public class RecentFile(string fullPath)
{
    public string FullPath => fullPath;

    public string Name => Path.GetFileName(this.FullPath);
}