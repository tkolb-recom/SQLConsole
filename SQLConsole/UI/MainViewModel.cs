using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.Win32;
using Recom.SQLConsole.Database;
using Recom.SQLConsole.Properties;

namespace Recom.SQLConsole.UI;

public partial class MainViewModel : ObservableObject
{
    private const string FilterString = "SQL Dateien (*.sql)|*.sql|Alle Dateien (*.*)|*.*";

    public MainViewModel()
    {
        IEnumerable<RecentFile> recentFiles = Settings.Default.RecentFiles.Split(';')
                                                        .Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f))
                                                        .Select(f => new RecentFile(f));

        foreach (RecentFile file in recentFiles)
        {
            this.RecentFiles.Add(file);
        }

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
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private TextDocument? _queryDocument = new TextDocument();

    public string WindowTitle
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var productAttr = assembly.GetCustomAttribute<AssemblyProductAttribute>();
            string title = productAttr?.Product ?? assembly.GetName().Name ?? string.Empty;

            if (this.QueryDocument?.FileName != null)
            {
                title += " - " + Path.GetFileName(this.QueryDocument?.FileName);
            };

            return title;
        }
        //set => this.SetProperty(ref field, value);
    }

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

    public ObservableCollection<RecentFile> RecentFiles { get; } = new();

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

        this.OpenFile(ofd.FileName);
    }

    [RelayCommand]
    public void OpenRecentFile(RecentFile file)
    {
        this.OpenFile(file.FullPath);
    }

    private void OpenFile(string filename)
    {
        this.UpdateRecentFiles(filename);

        this.QueryDocument = new TextDocument
        {
            FileName = filename,
            Text = File.ReadAllText(filename)
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

        this.UpdateRecentFiles(sfd.FileName);

        this.QueryDocument.FileName = sfd.FileName;
        return true;
    }

    private void UpdateRecentFiles(string filename)
    {
        RecentFile? existing = this.RecentFiles.FirstOrDefault(entry => Equals(entry.FullPath, filename));

        if (existing != null)
        {
            this.RecentFiles.Move(this.RecentFiles.IndexOf(existing), 0);
            return;
        }

        this.RecentFiles.Insert(0, new RecentFile(filename));
        if (this.RecentFiles.Count > 10)
        {
            this.RecentFiles.RemoveAt(10);
        }

        Settings.Default.RecentFiles = string.Join(";", this.RecentFiles.Select(f => f.FullPath));
        Settings.Default.Save();
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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DisconnectDatabaseCommand), nameof(RunScriptCommand))]
    private bool _isRunningScript;

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

    public bool CanDisconnectDatabase() => this.HasActiveDatabase && !this.IsRunningScript;

    [RelayCommand(CanExecute = nameof(CanRunScript))]
    public void RunScript()
    {
        if (this.ActiveDatabase == null || this.QueryDocument == null)
        {
            return;
        }

        try
        {
            this.ExecuteStatementAsync();
        }
        catch (Exception e)
        {
            MessageBox.Show(e.ToString(), "Fehler beim Ausführen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteStatementAsync()
    {
        string sql = this.QueryDocument!.Text;
        SqlDatabase db = this.ActiveDatabase!;

        Task.Run(ExecuteStatement).ContinueWith(DisplayResult, TaskScheduler.FromCurrentSynchronizationContext());

        return;

        (DataTable? data, int? affected) ExecuteStatement()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => this.IsRunningScript = true);

                Transaction? transaction = this.StartTransaction
                                               ? db.BeginTransaction(IsolationLevel.ReadUncommitted)
                                               : null;

                using var command = new SqlCommand(sql);
                command.Execute(db);

                var result = (data: (DataTable?)null, affected: (int?)0);

                if (command.ResultReader != null)
                {
                    DataTable data = new();
                    data.Load(command.ResultReader);
                    result.data = data;
                }
                else
                {
                    result.affected = command.AffectedRows;
                }

                if (this.CommitTransaction)
                {
                    transaction?.Commit();
                }

                return result;
            }
            finally
            {
                if (this.StartTransaction)
                {
                    db.CloseTransaction();
                }

                Application.Current.Dispatcher.Invoke(() => this.IsRunningScript = false);
            }
        }

        void DisplayResult(Task<(DataTable? data, int? affected)> t)
        {
            if (t.IsFaulted)
            {
                MessageBox.Show(t.Exception?.GetBaseException().Message ?? "Unbekannter Fehler", "Fehler beim Ausführen",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            (DataTable? data, int? affected) res = t.Result;

            if (res.data != null)
            {
                var resultWindow = new ResultWindow(sql, res.data);
                resultWindow.ShowDialog();
            }
            else if (res.affected > 0)
            {
                MessageBox.Show($"Betroffene Zeilen: {res.affected}", "Betroffene Zeilen", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Es wurden keine Ergebnisse zurückgegeben", "Keine Ergebnisse", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }

    public bool CanRunScript() => this.HasActiveDatabase
                                  && !this.IsRunningScript
                                  && !string.IsNullOrWhiteSpace(this.QueryDocument?.Text);

    #endregion
}