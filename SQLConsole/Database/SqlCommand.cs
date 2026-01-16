using System.Data;
using System.Text.RegularExpressions;

namespace Recom.SQLConsole.Database;

public partial class SqlCommand(string statement) : IDisposable
{
    public int? AffectedRows { get; private set; }

    public IDataReader? ResultReader { get; private set; }

    [GeneratedRegex(@"^\s*?(select|with|restore|--\s*?select)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex IsSelectStatementRegex { get; }

    public bool IsSelectStatement => IsSelectStatementRegex.IsMatch(this.Statement);

    public string Statement { get; } = statement;

    public void Execute(SqlDatabase database, int timeout = 30)
    {
        IDbConnection? connection = database.CurrentTransaction?.DbTransaction?.Connection
                                    ?? database.Connection;

        if (connection == null)
        {
            throw new InvalidOperationException("No connection to database!");
        }

        using IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = this.Statement;
        cmd.Transaction = database.CurrentTransaction?.DbTransaction;
        cmd.CommandTimeout = timeout;

        if (this.IsSelectStatement)
        {
            this.ResultReader = cmd.ExecuteReader(CommandBehavior.KeyInfo);
        }
        else
        {
            this.AffectedRows = cmd.ExecuteNonQuery();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.ResultReader is { IsClosed: false })
        {
            this.ResultReader.Close();
        }

        this.ResultReader?.Dispose();
        this.ResultReader = null;
    }
}