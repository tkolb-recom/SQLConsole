using DevExpress.Mvvm;
using Recom.SQLConsole.Database;

namespace Recom.SQLConsole.UI;

public partial class EditDatabaseConfigViewModel : ObservableObject, ISupportServices
{
    [ObservableProperty]
    private bool _integratedSecurity = false;

    partial void OnIntegratedSecurityChanged(bool value)
    {
        if (value)
        {
            this.SelectedDatabaseConfig?.Username = null;
            this.SelectedDatabaseConfig?.Password = null;
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    [NotifyPropertyChangedFor(nameof(IntegratedSecurity))]
    private DatabaseConfiguration? _selectedDatabaseConfig;

    public EditDatabaseConfigViewModel()
    {
        this.ServiceContainer = new ServiceContainer(this);
    }

    partial void OnSelectedDatabaseConfigChanging(DatabaseConfiguration? value)
    {
        if (value != null)
        {
            _integratedSecurity = string.IsNullOrWhiteSpace(value.Username) && string.IsNullOrWhiteSpace(value.Password);
        }
    }

    public ObservableCollection<DatabaseConfiguration> Configurations { get; } = new();

    [RelayCommand]
    public void Add()
    {
        var databaseConfiguration = new DatabaseConfiguration { Database = "Neu" };
        this.Configurations.Add(databaseConfiguration);
        this.SelectedDatabaseConfig = databaseConfiguration;
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    public void Remove()
    {
        if (this.SelectedDatabaseConfig == null)
        {
            return;
        }

        int index = this.Configurations.IndexOf(this.SelectedDatabaseConfig);
        this.Configurations.Remove(this.SelectedDatabaseConfig);
        this.SelectedDatabaseConfig = index < this.Configurations.Count
                                          ? this.Configurations[index]
                                          : index - 1 < this.Configurations.Count && index > 0
                                              ? this.Configurations[index - 1]
                                              : null;
    }

    public bool CanRemove() => this.SelectedDatabaseConfig != null;


    [RelayCommand]
    public void CloseDialog()
    {
        this.CurrentDialogService.Close(MessageResult.OK);
    }

    protected ICurrentDialogService CurrentDialogService => this.ServiceContainer.GetService<ICurrentDialogService>();

    /// <inheritdoc />
    public IServiceContainer ServiceContainer { get; }
}