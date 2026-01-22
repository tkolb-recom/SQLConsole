namespace Recom.SQLConsole.UI;

public partial class EditDatabaseConfigurationWindow : Window
{
    public EditDatabaseConfigurationWindow()
    {
        InitializeComponent();

        this.ViewModel = (EditDatabaseConfigViewModel)this.DataContext;
    }

    public EditDatabaseConfigViewModel ViewModel { get; private set; }
}