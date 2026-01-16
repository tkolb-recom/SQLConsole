using System.Data;

namespace Recom.SQLConsole.Database;

public class Transaction(SqlDatabase database) : IDisposable
{
    private IDbTransaction? _dbTransaction;
    private bool _hasBeenCommitted;
    private bool _hasBeenRolledBack;

    public IDbTransaction? DbTransaction => _dbTransaction;

    public IDbTransaction? Begin(IsolationLevel level)
    {
        IDbConnection connection = database.CreateConnection();
        connection.Open();

        _dbTransaction = connection.BeginTransaction(level);
        return this.DbTransaction;
    }

    public void Commit()
    {
        if (_hasBeenCommitted || _hasBeenRolledBack)
        {
            throw new SystemException(
                $"Can't rollback already {(_hasBeenCommitted ? "committed" : "rolled back")} transaction. Looks like a bug!");
        }

        try
        {
            if (this.DbTransaction != null)
            {
                this.DbTransaction.Commit();
                _hasBeenCommitted = true;
            }
        }
        finally
        {
            this.CleanUp();
        }
    }

    public void Rollback()
    {
        if (_hasBeenCommitted || _hasBeenRolledBack)
        {
            throw new SystemException(
                $"Can't rollback already {(_hasBeenCommitted ? "committed" : "rolled back")} transaction. Looks like a bug!");
        }

        try
        {
            if (this.DbTransaction != null)
            {
                this.DbTransaction.Rollback();
                _hasBeenRolledBack = true;
            }
        }
        finally
        {
            this.CleanUp();
        }
    }

    private void CleanUp()
    {
        _dbTransaction?.Connection?.Close();
        _dbTransaction?.Connection?.Dispose();

        _dbTransaction?.Dispose();
        _dbTransaction = null;
    }

    ~Transaction()
    {
        this.Dispose(false);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        if (_dbTransaction != null)
        {
            // Rollback performs cleanup
            this.Rollback();

            _dbTransaction = null;

            if (this.Equals(database.CurrentTransaction))
            {
                database.CloseTransaction();
            }
        }
    }
}