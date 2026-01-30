using Recom.SQLConsole.Properties;

namespace Recom.SQLConsole.UI;

public partial class SettingsViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    public SettingsViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        this.LoadSettings();
    }

    #region Database Configuration

    [ObservableProperty]
    private bool _portSelect;

    partial void OnPortSelectChanged(bool value)
    {
        this.SelectedDatabaseConfig?.Port = value ? 1433 : null;
    }

    [ObservableProperty]
    private bool _integratedSecurity;

    partial void OnIntegratedSecurityChanged(bool value)
    {
        if (value)
        {
            this.SelectedDatabaseConfig?.Username = null;
            this.SelectedDatabaseConfig?.Password = null;
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveDatabaseCommand))]
    [NotifyPropertyChangedFor(nameof(IntegratedSecurity))]
    private DatabaseConfigViewModel? _selectedDatabaseConfig;

    partial void OnSelectedDatabaseConfigChanging(DatabaseConfigViewModel? value)
    {
        if (value != null)
        {
            _integratedSecurity = string.IsNullOrWhiteSpace(value.Username) && string.IsNullOrWhiteSpace(value.Password);
        }
    }

    public ObservableCollection<DatabaseConfigViewModel> DatabaseConfigurations { get; } = new();

    [RelayCommand]
    public void AddDatabase()
    {
        var databaseConfiguration = new DatabaseConfigViewModel { Host = "Neu" };
        this.DatabaseConfigurations.Add(databaseConfiguration);
        this.SelectedDatabaseConfig = databaseConfiguration;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveDatabase))]
    public void RemoveDatabase()
    {
        if (this.SelectedDatabaseConfig == null)
        {
            return;
        }

        int index = this.DatabaseConfigurations.IndexOf(this.SelectedDatabaseConfig);
        this.DatabaseConfigurations.Remove(this.SelectedDatabaseConfig);
        this.SelectedDatabaseConfig = index < this.DatabaseConfigurations.Count
                                          ? this.DatabaseConfigurations[index]
                                          : index - 1 < this.DatabaseConfigurations.Count && index > 0
                                              ? this.DatabaseConfigurations[index - 1]
                                              : null;
    }

    public bool CanRemoveDatabase() => this.SelectedDatabaseConfig != null;

    #endregion

    #region Release Configuration

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveReleaseCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedDatabaseConfigOfRelease))]
    private ReleaseConfigViewModel? _selectedReleaseConfig;

    partial void OnSelectedReleaseConfigChanged(ReleaseConfigViewModel? value)
    {
        this.SelectedDatabaseConfigOfRelease = this.DatabaseConfigurations.FirstOrDefault(x => value?.DatabaseHost == x.Host);
    }

    public ObservableCollection<ReleaseConfigViewModel> ReleaseConfigurations { get; } = new();

    [RelayCommand]
    public void AddRelease()
    {
        var releaseConfiguration = new ReleaseConfigViewModel { Name = "Neu" };
        this.ReleaseConfigurations.Add(releaseConfiguration);
        this.SelectedReleaseConfig = releaseConfiguration;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveRelease))]
    public void RemoveRelease()
    {
        if (this.SelectedReleaseConfig == null)
        {
            return;
        }

        int index = this.ReleaseConfigurations.IndexOf(this.SelectedReleaseConfig);
        this.ReleaseConfigurations.Remove(this.SelectedReleaseConfig);
        this.SelectedReleaseConfig = index < this.ReleaseConfigurations.Count
                                         ? this.ReleaseConfigurations[index]
                                         : index - 1 < this.ReleaseConfigurations.Count && index > 0
                                             ? this.ReleaseConfigurations[index - 1]
                                             : null;
    }

    public bool CanRemoveRelease() => this.SelectedReleaseConfig != null;

    [ObservableProperty]
    DatabaseConfigViewModel? _selectedDatabaseConfigOfRelease;

    partial void OnSelectedDatabaseConfigOfReleaseChanged(DatabaseConfigViewModel? value)
    {
        this.SelectedReleaseConfig?.DatabaseHost = value?.Host;
    }

    #endregion

    #region Repository

    [ObservableProperty]
    string? _repositoryPath;

    #endregion

    [RelayCommand]
    public void SaveAndClose()
    {
        this.SaveSettings();

        _navigationService.Close(this, true);
    }

    private void SaveSettings()
    {
        Settings.Default.Databases = new List<DatabaseConfiguration>(this.DatabaseConfigurations.Select(c => c.AsSettings()));
        Settings.Default.Releases = new List<ReleaseConfiguration>(this.ReleaseConfigurations.Select(c => c.AsSettings()));
        Settings.Default.RepositoryPath = this.RepositoryPath;

        Settings.Default.Save();
    }

    private void LoadSettings()
    {
        if (Settings.Default.Databases != null)
        {
            foreach (DatabaseConfiguration configuration in Settings.Default.Databases)
            {
                this.DatabaseConfigurations.Add(DatabaseConfigViewModel.FromSettings(configuration));
            }
        }

        this.SelectedDatabaseConfig = this.DatabaseConfigurations.FirstOrDefault();

        if (Settings.Default.Releases != null)
        {
            foreach (ReleaseConfiguration release in Settings.Default.Releases)
            {
                this.ReleaseConfigurations.Add(ReleaseConfigViewModel.FromSettings(release));
            }
        }

        this.SelectedReleaseConfig = this.ReleaseConfigurations.FirstOrDefault();

        this.RepositoryPath = Settings.Default.RepositoryPath;
    }
}