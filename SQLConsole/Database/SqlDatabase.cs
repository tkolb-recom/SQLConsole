using System.Data;
using Microsoft.Data.SqlClient;
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

    public IDbConnection Connect() => this.Connection = this.CreateConnection();

    public IDbConnection CreateConnection() => new SqlConnection(this.Configuration.ConnectionString);

    public void Disconnect() => this.Connection?.Close();

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