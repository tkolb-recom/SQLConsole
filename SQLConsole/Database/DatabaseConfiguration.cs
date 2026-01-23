using System.Reflection;
using System.Xml.Serialization;
using Microsoft.Data.SqlClient;

namespace Recom.SQLConsole.Database;

[Serializable]
public class DatabaseConfiguration
{
    [XmlIgnore]
    public Guid Id { get; private init; } = Guid.NewGuid();

    /// <summary>
    /// Name of the database
    /// </summary>
    [XmlAttribute]
    public string? Database { get; set; }

    /// <summary>
    /// Host that the database is running on
    /// </summary>
    [XmlAttribute]
    public string? Host { get; set; }

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

    public string ConnectionString => this.CreateConnectionString();

    private string CreateConnectionString()
    {
        string? host = this.Host?.Replace(':', ',');

        SqlConnectionStringBuilder cs = new SqlConnectionStringBuilder
        {
            DataSource = host,
            InitialCatalog = this.Database,
            ApplicationName = Assembly.GetExecutingAssembly().FullName,
            ConnectTimeout = this.Timeout,
            /*
            MultipleActiveResultSets = true
            Pooling = Pooling,
            Encrypt = EncryptEnabled
            */
            TrustServerCertificate = true
        };

        if (string.IsNullOrEmpty(this.Username) && string.IsNullOrEmpty(this.Password))
        {
            cs.IntegratedSecurity = true; // Windows Auth.
        }
        else
        {
            cs.UserID = this.Username;
            cs.Password = this.Password;
        }

        /* force escape the database name to fix problems with hyphens (-) and allow db names like grips-fr */
        return Regex.Replace(cs.ConnectionString, "(Initial Catalog=)([^;]+)", "$1'$2'");
    }
}
