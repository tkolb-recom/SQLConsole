using System.Reflection;
using Microsoft.Data.SqlClient;
using Recom.SQLConsole.Properties;
using IsolationLevel = System.Data.IsolationLevel;

namespace Recom.SQLConsole.Database;

/// <summary>
/// Represents a SQL database and provides methods for managing
/// transactions and connections to the database.
/// </summary>
public class SqlDatabase(DatabaseConfiguration configuration) : IDisposable
{
    public DatabaseConfiguration Configuration { get; private set; } = configuration;

    public IDbConnection? Connection { get; private set; }

    public string? DatabaseName { get; private set; }

    public IDbConnection Connect(string database)
        => this.Connection = this.CreateConnection(database);

    public IDbConnection CreateConnection(string database)
        => new SqlConnection(this.CreateConnectionString(database));

    public void Disconnect() => this.Connection?.Close();

    private string CreateConnectionString(string database)
    {
        this.DatabaseName = database;
        string host = $"{this.Configuration.Host}{(this.Configuration.Port > 0 ? $",{this.Configuration.Port}" : "")}";

        SqlConnectionStringBuilder cs = new SqlConnectionStringBuilder
        {
            DataSource = host,
            InitialCatalog = database,
            ApplicationName = Assembly.GetExecutingAssembly().FullName,
            ConnectTimeout = this.Configuration.Timeout,
            /*
            MultipleActiveResultSets = true
            Pooling = Pooling,
            Encrypt = EncryptEnabled
            */
            TrustServerCertificate = true
        };

        if (string.IsNullOrEmpty(this.Configuration.Username)
            && string.IsNullOrEmpty(this.Configuration.Password))
        {
            cs.IntegratedSecurity = true; // Windows Auth.
        }
        else
        {
            cs.UserID = this.Configuration.Username;
            cs.Password = this.Configuration.Password;
        }

        /* force escape the database name to fix problems with hyphens (-) and allow db names like grips-fr */
        return Regex.Replace(cs.ConnectionString, "(Initial Catalog=)([^;]+)", "$1'$2'");
    }

    public Transaction BeginTransaction(IsolationLevel level = IsolationLevel.Unspecified)
    {
        if (this.HasTransaction)
        {
            throw new InvalidOperationException("Transaction already open. Nested Transaction not allowed!");
        }

        try
        {
            this.CurrentTransaction = new Transaction(this);
            this.CurrentTransaction.Begin(level);
            return this.CurrentTransaction;
        }
        catch (Exception)
        {
            this.CurrentTransaction?.Dispose();
            this.CurrentTransaction = null;
            throw;
        }
    }

    public void CloseTransaction()
    {
        if (!this.HasTransaction)
        {
            throw new InvalidOperationException("There is no active transaction.");
        }

        try
        {
            this.CurrentTransaction?.Dispose();
        }
        finally
        {
            this.CurrentTransaction = null;
        }
    }

    public bool HasTransaction => this.CurrentTransaction != null;

    public Transaction? CurrentTransaction { get; private set; }

    /// <inheritdoc />
    public void Dispose()
    {
        this.Disconnect();
        this.CurrentTransaction?.Dispose();
        this.Connection?.Dispose();
    }
}