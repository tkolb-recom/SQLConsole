using Recom.SQLConsole.Database;

namespace Recom.SQLConsole.UI;

public partial class DatabaseConfigViewModel : ObservableObject
{
    public Guid Id { get; private init; } = Guid.NewGuid();

    public DatabaseConfiguration? Settings { get; private init; }

    /// <summary>
    /// Creates a deep copy of the configuration for the view models.
    /// </summary>
    public static DatabaseConfigViewModel FromSettings(Recom.SQLConsole.Database.DatabaseConfiguration settings)
    {
        return new DatabaseConfigViewModel
        {
            Settings = settings,
            Id = settings.Id,
            Database = settings.Database,
            Host = settings.Host,
            Username = settings.Username,
            Password = settings.Password,
            Timeout = settings.Timeout
        };
    }

    /// <summary>
    /// Copies the values into the settings object after editing.
    /// </summary>
    public DatabaseConfiguration AsSettings()
    {
        DatabaseConfiguration settings = this.Settings ?? new DatabaseConfiguration();
        settings.Database = this.Database;
        settings.Host = this.Host;
        settings.Username = this.Username;
        settings.Password = this.Password;
        settings.Timeout = this.Timeout;

        return settings;
    }

    [ObservableProperty]
    private string? _database;

    [ObservableProperty]
    private string? _host;

    [ObservableProperty]
    private string? _username;

    [ObservableProperty]
    private string? _password;

    [ObservableProperty]
    private int _timeout;
}