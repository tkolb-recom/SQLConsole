using Recom.SQLConsole.Properties;

namespace Recom.SQLConsole.UI;

public partial class ReleaseConfigViewModel : ObservableObject
{
    public Guid Id { get; private init; } = Guid.NewGuid();

    public ReleaseConfiguration? Settings { get; private init; }

    /// <summary>
    /// Creates a deep copy of the configuration for the view models.
    /// </summary>
    public static ReleaseConfigViewModel FromSettings(ReleaseConfiguration settings)
    {
        return new ReleaseConfigViewModel
        {
            Settings = settings,
            Id = settings.Id,
            Name = settings.Name,
            DatabaseHost = settings.DatabaseHost,
            DatabaseName = settings.DatabaseName,
            RepositoryBranch = settings.RepositoryBranch,
        };
    }

    /// <summary>
    /// Copies the values into the settings object after editing.
    /// </summary>
    public ReleaseConfiguration AsSettings()
    {
        ReleaseConfiguration settings = this.Settings ?? new ReleaseConfiguration();
        settings.Name = this.Name;
        settings.DatabaseHost = this.DatabaseHost;
        settings.DatabaseName = this.DatabaseName;
        settings.RepositoryBranch = this.RepositoryBranch;

        return settings;
    }

    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private string? _databaseHost;

    [ObservableProperty]
    private string? _databaseName;

    [ObservableProperty]
    private string? _repositoryBranch;
}