using System.Windows;
using Recom.SQLConsole.Highlighting;

namespace Recom.SQLConsole;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        HighlightingHelper.RegisterHighlighting("SQL", ".sql", "TSQL-Mode.xshd");

        base.OnStartup(e);
    }
}