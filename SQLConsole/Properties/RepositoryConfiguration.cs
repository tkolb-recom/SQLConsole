using System.Xml.Serialization;

namespace Recom.SQLConsole.Properties;

[Serializable]
public class RepositoryConfiguration
{
    /// <summary>
    /// Path to the repository.
    /// </summary>
    [XmlAttribute]
    public string? Path { get; set; }
}