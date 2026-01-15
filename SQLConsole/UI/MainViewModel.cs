using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Document;

namespace Recom.SQLConsole.UI;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    TextDocument? _queryDocument = new TextDocument();

    public string? Query => this.QueryDocument?.Text;
}