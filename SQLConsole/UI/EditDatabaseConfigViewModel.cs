using Recom.SQLConsole.Database;

namespace Recom.SQLConsole.UI;

public partial class EditDatabaseConfigViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    private DatabaseConfiguration? _selectedDatabaseConfig;

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
}