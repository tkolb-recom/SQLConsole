using System.Data;
using System.Windows;

namespace Recom.SQLConsole.UI;

public partial class ResultWindow : Window
{
    public ResultWindow(string statement, DataTable data)
    {
        InitializeComponent();

        this.ViewModel = (ResultViewModel)this.DataContext;
        this.ViewModel.Data = data;
        this.ViewModel.Statement = statement;
    }

    public ResultViewModel ViewModel { get; set; }
}