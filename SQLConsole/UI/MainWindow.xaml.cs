using System.Windows.Controls.Ribbon;

namespace Recom.SQLConsole.UI;

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