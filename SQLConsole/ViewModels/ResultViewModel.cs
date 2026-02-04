using ICSharpCode.AvalonEdit.Document;

namespace Recom.SQLConsole.ViewModels;

public partial class ResultViewModel : ObservableObject
{
    [ObservableProperty]
    private TextDocument? _queryDocument = new TextDocument();

    [ObservableProperty]
    private DataTable? _data;

    public string? Statement
    {
        get;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                this.QueryDocument = new TextDocument(value ?? string.Empty);
            }
        }
    }
}