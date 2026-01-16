using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.Win32;

namespace Recom.SQLConsole.UI;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    TextDocument? _queryDocument = new TextDocument();

    public string? Query => this.QueryDocument?.Text;

    [RelayCommand]
    public void Exit()
    {
        Application.Current.Shutdown();
    }

    [RelayCommand]
    public void NewDocument()
    {
        this.QueryDocument = new TextDocument();
    }

    [RelayCommand]
    public void OpenDocument()
    {
        var ofd = new OpenFileDialog
        {
            DefaultExt = "sql",
            Filter = FilterString
        };
        if (!ofd.ShowDialog().GetValueOrDefault())
        {
            return;
        }

        this.QueryDocument = new TextDocument
        {
            FileName = ofd.FileName,
            Text = File.ReadAllText(ofd.FileName)
        };
    }

    [RelayCommand]
    public void SaveDocument()
    {
        if (this.QueryDocument == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(this.QueryDocument.FileName))
        {
            if (!this.SelectFilename())
            {
                return;
            }
        }

        using FileStream fs = new FileStream(this.QueryDocument.FileName, FileMode.Create);
        using TextWriter tw = new StreamWriter(fs);
        this.QueryDocument.WriteTextTo(tw);
    }

    [RelayCommand]
    public void SaveDocumentAs()
    {
        if (this.QueryDocument == null)
        {
            return;
        }

        if (!this.SelectFilename())
        {
            return;
        }

        using FileStream fs = new FileStream(this.QueryDocument.FileName, FileMode.Create);
        using TextWriter tw = new StreamWriter(fs);
        this.QueryDocument.WriteTextTo(tw);
    }

    private bool SelectFilename()
    {
        if (this.QueryDocument == null)
        {
            return false;
        }

        var sfd = new SaveFileDialog
        {
            DefaultExt = "sql",
            Filter = FilterString
        };
        if (!sfd.ShowDialog().GetValueOrDefault())
        {
            return false;
        }

        this.QueryDocument.FileName = sfd.FileName;
        return true;
    }

    private const string FilterString = "SQL Dateien (*.sql)|*.sql|Alle Dateien (*.*)|*.*";
}