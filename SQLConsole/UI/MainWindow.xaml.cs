using System.Windows;
using System.Windows.Controls.Ribbon;
using ICSharpCode.AvalonEdit.Highlighting;

namespace Recom.SQLConsole.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : RibbonWindow
{
    public MainWindow()
    {
        IHighlightingDefinition? sql = HighlightingManager.Instance.GetDefinition("SQL");

        this.InitializeComponent();
    }
}