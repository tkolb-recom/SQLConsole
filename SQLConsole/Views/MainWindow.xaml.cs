using System.Windows.Controls.Ribbon;
using Recom.SQLConsole.ViewModels;

namespace Recom.SQLConsole.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : RibbonWindow
{
    public MainWindow()
    {
        this.InitializeComponent();

        this.DataContext = Dependencies.Get<MainViewModel>()!;
    }
}