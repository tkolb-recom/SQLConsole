using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Recom.SQLConsole.Database;

public partial class DatabaseConfiguration : ObservableObject
{
    public Guid Id { get; private init; } = Guid.NewGuid();

    /// <summary>
    /// Creates a deep copy of this configuration for editing.
    /// </summary>
    public DatabaseConfiguration Copy()
    {
        return new DatabaseConfiguration
        {
            Id = this.Id,
            Database = this.Database,
            Host = this.Host,
            Username = this.Username,
            Password = this.Password,
            Timeout = this.Timeout
        };
    }

    /// <summary>
    /// Copies the values from another configuration into this one after editing.
    /// </summary>
    public void CopyFrom(DatabaseConfiguration other)
    {
        if (other.Id != this.Id)
        {
            throw new ArgumentException("Cannot copy from another configuration with a different ID.", nameof(other));
        }

        this.Database = other.Database;
        this.Host = other.Host;
        this.Username = other.Username;
        this.Password = other.Password;
        this.Timeout = other.Timeout;
    }

    /// <summary>
    /// Name of the database
    /// </summary>
    [ObservableProperty]
    private string? _database;

    /// <summary>
    /// Host that the database is running on
    /// </summary>
    [ObservableProperty]
    private string? _host;

    /// <summary>
    /// Username used to access the database.
    /// </summary>
    [ObservableProperty]
    private string? _username;

    /// <summary>
    /// Password used to access the database.
    /// </summary>
    [ObservableProperty]
    private string? _password;

    /// <summary>
    /// Timeout duration, in seconds, for database connection attempts.
    /// </summary>
    [ObservableProperty]
    private int _timeout;

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