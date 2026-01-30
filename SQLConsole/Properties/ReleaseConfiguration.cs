using System.Xml.Serialization;

namespace Recom.SQLConsole.Properties;

[Serializable]
public class ReleaseConfiguration
{
    [XmlIgnore]
    public Guid Id { get; private init; } = Guid.NewGuid();

    /// <summary>
    /// Name of the release.
    /// </summary>
    [XmlAttribute]
    public string? Name { get; set; }

    /// <summary>
    /// Name of the database configuration object. (see <see cref="DatabaseConfiguration.Host"/>)
    /// </summary>
    [XmlAttribute]
    public string? DatabaseHost { get; set; }

    /// <summary>
    /// Name of the database to connect to.
    /// </summary>
    [XmlAttribute]
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Branch in the repository that is used for the release.
    /// </summary>
    [XmlAttribute]
    public string? RepositoryBranch { get; set; }
}