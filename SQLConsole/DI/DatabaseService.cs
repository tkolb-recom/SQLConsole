using Recom.SQLConsole.Database;
using Recom.SQLConsole.Properties;

namespace Recom.SQLConsole.DI;

public class DatabaseService
{
    public DatabaseConfiguration? Configuration { get; set; }

    public SqlDatabase? ActiveDatabase { get; private set; }

    public void ConnectToDatabase(DatabaseConfiguration configuration, string databaseName)
    {
        if (this.ActiveDatabase != null)
        {
            throw new InvalidOperationException("Database already connected.");
        }

        this.ActiveDatabase = new SqlDatabase(configuration);
        this.ActiveDatabase.Connect(databaseName);
    }

    public void DisconnectFromDatabase()
    {
        this.ActiveDatabase?.Disconnect();
        this.ActiveDatabase?.Dispose();
        this.ActiveDatabase = null;
    }

    public void ExecuteSql(string sql, bool startTransaction = true, bool commitTransaction = false)
    {
        if (this.ActiveDatabase == null)
        {
            throw new InvalidOperationException("No database connected.");
        }

        Transaction? transaction = null;
        try
        {
            this.LastAffectedRows = null;
            this.LastData?.Dispose();
            this.LastData = null;

            SqlDatabase? db = this.ActiveDatabase;

            transaction = startTransaction
                              ? db.BeginTransaction(IsolationLevel.ReadUncommitted)
                              : null;

            using var command = new SqlCommand(sql);
            command.Execute(db);

            if (command.ResultReader != null)
            {
                DataTable data = new();
                data.Load(command.ResultReader);
                this.LastData = data;
            }
            else
            {
                this.LastAffectedRows = command.AffectedRows;
            }
        }
        finally
        {
            if (commitTransaction)
            {
                transaction?.Commit();
            }
        }
    }

    public int? LastAffectedRows { get; private set; }

    public DataTable? LastData { get; private set; }

}