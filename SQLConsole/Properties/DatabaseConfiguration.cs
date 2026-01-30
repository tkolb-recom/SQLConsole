using System.Xml.Serialization;

namespace Recom.SQLConsole.Properties;

[Serializable]
public class DatabaseConfiguration
{
    [XmlIgnore]
    public Guid Id { get; private init; } = Guid.NewGuid();

    /// <summary>
    /// Host that the database is running on
    /// </summary>
    [XmlAttribute]
    public string? Host { get; set; }

    /// <summary>
    /// Deviating port of the database
    /// </summary>
    [XmlAttribute]
    public int Port { get; set; }

    /// <summary>
    /// Username used to access the database.
    /// </summary>
    [XmlAttribute]
    public string? Username { get; set; }

    /// <summary>
    /// Password used to access the database.
    /// </summary>
    [XmlAttribute]
    public string? Password { get; set; }

    /// <summary>
    /// Timeout duration, in seconds, for database connection attempts.
    /// </summary>
    [XmlAttribute]
    public int Timeout { get; set; }
}