using Recom.SQLConsole.ViewModels;

namespace Recom.SQLConsole.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        this.ViewModel = (SettingsViewModel)this.DataContext;
    }

    public SettingsViewModel ViewModel { get; private set; }
}