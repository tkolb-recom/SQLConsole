namespace Recom.SQLConsole.UI;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        this.ViewModel = (SettingsViewModel)this.DataContext;
    }

    public SettingsViewModel ViewModel { get; private set; }
}