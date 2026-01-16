using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.Win32;
using Recom.SQLConsole.Database;

namespace Recom.SQLConsole.UI;

public partial class MainViewModel : ObservableObject
{
    private const string FilterString = "SQL Dateien (*.sql)|*.sql|Alle Dateien (*.*)|*.*";

    public MainViewModel()
    {
        this.Databases.Add(new DatabaseConfiguration
        {
            Database = "MasterContent",
            Host = "vidab-2",
            UserName = "sa",
            Password = "dev_sa",
            TimeOut = 30
        });

        this.SelectedDatabaseConfig = this.Databases.First();
    }

    [ObservableProperty]
    private TextDocument? _queryDocument = new TextDocument();

    [RelayCommand]
    public void OnQueryTextChanged()
    {
        this.RunScriptCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    public void Exit()
    {
        Application.Current.Shutdown();
    }

    #region File management

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

    #endregion

    #region Database handling

    public ObservableCollection<DatabaseConfiguration> Databases { get; } = new ObservableCollection<DatabaseConfiguration>();

    [ObservableProperty]
    DatabaseConfiguration? _selectedDatabaseConfig;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectDatabaseCommand), nameof(DisconnectDatabaseCommand), nameof(RunScriptCommand))]
    private SqlDatabase? _activeDatabase;

    [ObservableProperty]
    private bool _startTransaction = true;

    [ObservableProperty]
    private bool _commitTransaction;

    public bool HasActiveDatabase => this.ActiveDatabase != null;

    [RelayCommand(CanExecute = nameof(CanConnectDatabase))]
    public void ConnectDatabase()
    {
        try
        {
            this.ActiveDatabase = new SqlDatabase(this.SelectedDatabaseConfig!);
            this.ActiveDatabase.Connect();
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Fehler beim Verbinden", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public bool CanConnectDatabase() => this.SelectedDatabaseConfig != null && !this.HasActiveDatabase;

    [RelayCommand(CanExecute = nameof(CanDisconnectDatabase))]
    public void DisconnectDatabase()
    {
        if (this.ActiveDatabase == null)
        {
            return;
        }

        try
        {
            this.ActiveDatabase.Disconnect();
            this.ActiveDatabase.Dispose();
            this.ActiveDatabase = null;
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Fehler beim Trennen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public bool CanDisconnectDatabase() => this.HasActiveDatabase;

    [RelayCommand(CanExecute = nameof(CanRunScript))]
    public void RunScript()
    {
        if (this.ActiveDatabase == null || this.QueryDocument == null)
        {
            return;
        }

        try
        {
            Transaction? transaction = this.StartTransaction
                                           ? this.ActiveDatabase.BeginTransaction(IsolationLevel.ReadUncommitted)
                                           : null;

            using (var command = new SqlCommand(this.QueryDocument.Text))
            {
                command.Execute(this.ActiveDatabase);

                if (command.ResultReader != null)
                {
                    // TODO result?
                    var reader =  command.ResultReader;
                    int columns = reader.FieldCount;

                }
            }

            if (this.CommitTransaction)
            {
                transaction?.Commit();
            }

            if (this.StartTransaction)
            {
                this.ActiveDatabase.CloseTransaction();
            }
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Fehler beim Ausführen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public bool CanRunScript() => this.HasActiveDatabase && !string.IsNullOrWhiteSpace(this.QueryDocument?.Text);

    #endregion
}