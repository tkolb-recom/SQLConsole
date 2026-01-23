namespace Recom.SQLConsole.UI;

public partial class EditDatabaseConfigWindow : Window
{
    public EditDatabaseConfigWindow()
    {
        InitializeComponent();

        this.ViewModel = (EditDatabaseConfigViewModel)this.DataContext;
    }

    public EditDatabaseConfigViewModel ViewModel { get; private set; }
}