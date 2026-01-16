using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Recom.SQLConsole.Database;

public class DatabaseConfiguration
{
    /// <summary>
    /// Name of the database
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Host that the database is running on
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Username used to access the database.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Password used to access the database.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Timeout duration, in seconds, for database connection attempts.
    /// </summary>
    public int TimeOut { get; set; }

    public string ConnectionString => field ??= this.CreateConnectionString();

    private string CreateConnectionString()
    {
        string? host = this.Host?.Replace(':', ',');

        SqlConnectionStringBuilder cs = new SqlConnectionStringBuilder
        {
            DataSource = host,
            InitialCatalog = this.Database,
            ApplicationName = Assembly.GetExecutingAssembly().FullName,
            ConnectTimeout = this.TimeOut,
            /*
            MultipleActiveResultSets = true
            Pooling = Pooling,
            Encrypt = EncryptEnabled
            */
            TrustServerCertificate = true
        };

        if (string.IsNullOrEmpty(this.UserName) && string.IsNullOrEmpty(this.Password))
        {
            cs.IntegratedSecurity = true; // Windows Auth.
        }
        else
        {
            cs.UserID = this.UserName;
            cs.Password = this.Password;
        }

        /* force escape the database name to fix problems with hyphens (-) and allow db names like grips-fr */
        return Regex.Replace(cs.ConnectionString, "(Initial Catalog=)([^;]+)", "$1'$2'");
    }
}