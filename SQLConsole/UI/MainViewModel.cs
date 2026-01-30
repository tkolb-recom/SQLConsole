using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using ICSharpCode.AvalonEdit.Document;
using Recom.SQLConsole.Database;
using Recom.SQLConsole.Properties;

namespace Recom.SQLConsole.UI;

public partial class MainViewModel : ObservableObject
{
    private const string FilterStringConst = "SQL Dateien (*.sql)|*.sql|Alle Dateien (*.*)|*.*";

    public MainViewModel()
    {
        IEnumerable<RecentFile> recentFiles = Settings.Default.RecentFiles.Split(';')
                                                      .Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f))
                                                      .Select(f => new RecentFile(f));

        foreach (RecentFile file in recentFiles)
        {
            this.RecentFiles.Add(file);
        }

        this.SelectedFont = Settings.Default.RecentFont ?? "Consolas";
        this.SelectedFontSize = Settings.Default.RecentFontSize;

        this.LoadSettings();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditorVisible), nameof(IsUploadVisible))]
    int _selectedTabIndex;

    public bool IsEditorVisible => this.SelectedTabIndex < 2;

    public bool IsUploadVisible => this.SelectedTabIndex == 2;

    [RelayCommand]
    public void RibbonLoaded()
    {
        // ribbon combobox (mis)behavior on binding TwoWay correctly
        this.OnPropertyChanged(nameof(this.SelectedDatabaseConfig));
        this.OnPropertyChanged(nameof(this.SelectedFont));
        this.OnPropertyChanged(nameof(this.SelectedFontSize));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    [NotifyCanExecuteChangedFor(nameof(RunScriptCommand))]
    private TextDocument? _queryDocument = new TextDocument();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    [NotifyCanExecuteChangedFor(nameof(RunScriptCommand))]
    private bool _documentHasChanges;

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
            }

            if (this.DocumentHasChanges)
            {
                title += " *";
            }

            return title;
        }
        //set => this.SetProperty(ref field, value);
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
        if (this.CheckUnsavedChanged())
        {
            this.QueryDocument = new TextDocument();
            this.DocumentHasChanges = false;
        }
    }

    [RelayCommand]
    public void OpenDocument()
    {
        if (!this.CheckUnsavedChanged())
        {
            return;
        }

        var ofd = new OpenFileDialog
        {
            DefaultExt = "sql",
            Filter = FilterStringConst
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
        if (!this.CheckUnsavedChanged())
        {
            return;
        }

        this.OpenFile(file.FullPath);
    }

    private bool CheckUnsavedChanged()
    {
        if (!this.DocumentHasChanges)
        {
            return true;
        }

        MessageBoxResult result = MessageBox.Show("Die nicht gespeicherten Änderungen gehen verloren.", "Achtung",
            MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);

        return result == MessageBoxResult.OK;
    }

    private void OpenFile(string filename)
    {
        this.UpdateRecentFiles(filename);

        this.QueryDocument = new TextDocument
        {
            FileName = filename,
            Text = File.ReadAllText(filename)
        };
        this.DocumentHasChanges = false;
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
        this.DocumentHasChanges = false;
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
        this.DocumentHasChanges = false;
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
            Filter = FilterStringConst
        };
        if (!sfd.ShowDialog().GetValueOrDefault())
        {
            return false;
        }

        this.UpdateRecentFiles(sfd.FileName);

        this.QueryDocument.FileName = sfd.FileName;
        this.OnPropertyChanged(nameof(this.WindowTitle));

        return true;
    }

    private void UpdateRecentFiles(string filename)
    {
        RecentFile? existing = this.RecentFiles.FirstOrDefault(entry => Equals(entry.FullPath, filename));

        if (existing != null)
        {
            this.RecentFiles.Move(this.RecentFiles.IndexOf(existing), 0);
        }
        else
        {
            this.RecentFiles.Insert(0, new RecentFile(filename));
        }

        if (this.RecentFiles.Count > 10)
        {
            this.RecentFiles.RemoveAt(10);
        }

        Settings.Default.RecentFiles = string.Join(";", this.RecentFiles.Select(f => f.FullPath));
        Settings.Default.Save();
    }

    #endregion

    #region Font

    public ObservableCollection<string> AvailableFonts { get; }
        = ["Arial", "Consolas", "Courier New", "Times New Roman"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedFontFamily))]
    private string? _selectedFont = "Consolas";

    public FontFamily SelectedFontFamily
    {
        get
        {
            System.Diagnostics.Debug.WriteLine(this.SelectedFont ?? "null");
            return this.SelectedFont != null
                       ? field = new FontFamily(this.SelectedFont)
                       : field;
        }
    }

    public ObservableCollection<double> AvailableFontSizes { get; }
        = [8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24];

    [ObservableProperty]
    private double _selectedFontSize = 12;

    [RelayCommand]
    public void IncreaseFontSize()
    {
        if (this.SelectedFontSize < this.AvailableFontSizes.Max())
        {
            this.SelectedFontSize = this.AvailableFontSizes[this.AvailableFontSizes.IndexOf(this.SelectedFontSize) + 1];
        }
    }

    [RelayCommand]
    public void DecreaseFontSize()
    {
        if (this.SelectedFontSize > this.AvailableFontSizes.Min())
        {
            this.SelectedFontSize = this.AvailableFontSizes[this.AvailableFontSizes.IndexOf(this.SelectedFontSize) - 1];
        }
    }

    partial void OnSelectedFontChanged(string? value)
    {
        Settings.Default.RecentFont = value;
        Settings.Default.Save();
    }

    partial void OnSelectedFontSizeChanged(double value)
    {
        Settings.Default.RecentFontSize = value;
        Settings.Default.Save();
    }

    #endregion

    #region Settings

    private void LoadSettings()
    {
        this.Databases.Clear();
        this.Releases.Clear();

        if (Settings.Default.Databases != null)
        {
            foreach (DatabaseConfiguration configuration in Settings.Default.Databases)
            {
                this.Databases.Add(DatabaseConfigViewModel.FromSettings(configuration));
            }
        }

        if (Settings.Default.Releases != null)
        {
            foreach (ReleaseConfiguration release in Settings.Default.Releases)
            {
                this.Releases.Add(ReleaseConfigViewModel.FromSettings(release));
            }
        }

        this.SelectedReleaseConfig = this.Releases.FirstOrDefault();
    }

    [RelayCommand]
    public void OpenSettings()
    {
        var settingsViewModel = Dependencies.Get<SettingsViewModel>()!;

        Guid? releaseConfigId = this.SelectedReleaseConfig?.Id;
        if (this.SelectedReleaseConfig != null)
        {
            settingsViewModel.SelectedReleaseConfig =
                settingsViewModel.ReleaseConfigurations.FirstOrDefault(c => c.Id == this.SelectedReleaseConfig.Id);
        }

        var nav = Dependencies.Get<INavigationService>()!;
        if (nav.ShowDialog(settingsViewModel, this).GetValueOrDefault())
        {
            this.LoadSettings();

            this.SelectedReleaseConfig = this.Releases.FirstOrDefault(c => c.Id == releaseConfigId) ??
                                         this.Releases.FirstOrDefault();
        }
    }

    #endregion

    #region Release handling

    public ObservableCollection<ReleaseConfigViewModel> Releases { get; } = new ObservableCollection<ReleaseConfigViewModel>();

    [ObservableProperty]
    ReleaseConfigViewModel? _selectedReleaseConfig;

    partial void OnSelectedReleaseConfigChanged(ReleaseConfigViewModel? value)
    {
        this.DisconnectDatabase();

        if (value == null)
        {
            this.SelectedDatabaseConfig = null;
            return;
        }

        this.SelectedDatabaseConfig = this.Databases.FirstOrDefault(c => c.Host == value.DatabaseHost);
    }

    #endregion

    #region Database handling

    public ObservableCollection<DatabaseConfigViewModel> Databases { get; } = new ObservableCollection<DatabaseConfigViewModel>();

    [ObservableProperty]
    DatabaseConfigViewModel? _selectedDatabaseConfig;

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
            this.ActiveDatabase = new SqlDatabase(this.SelectedDatabaseConfig!.Settings!);
            this.ActiveDatabase.Connect("MasterContent"); // TODO from ReleaseConfig
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
                var resultViewModel = Dependencies.Get<ResultViewModel>()!;
                resultViewModel.Statement = sql;
                resultViewModel.Data = res.data;

                var nav = Dependencies.Get<INavigationService>()!;
                nav.Navigate(resultViewModel, this);
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

    #region Upload

    [ObservableProperty]
    private bool _preflightCheckPassed;

    public static string SqlFilterString => FilterStringConst;

    public ObservableCollection<UploadItem> UploadItems { get; } = new ObservableCollection<UploadItem>();

    [RelayCommand]
    public void AddUploadItem()
    {
        this.UploadItems.Add(new UploadItem());
        this.PreflightCheckPassed = false;
    }

    [RelayCommand]
    public void RemoveUploadItem(UploadItem item) => this.UploadItems.Remove(item);

    [RelayCommand]
    public void PreflightCheck()
    {
        this.PreflightCheckPassed = false;

        if (this.UploadItems.Any(x => x.Release == null || string.IsNullOrWhiteSpace(x.FilePath) || !File.Exists(x.FilePath)))
        {
            MessageBox.Show("Mindestens ein Eintrag ist nicht vollständig gültig ausgefüllt", "Fehler", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        if (this.UploadItems.DistinctBy(x => x.Release).Count() != this.UploadItems.Count)
        {
            MessageBox.Show("Verschiedene Einträge verweisen auf dasselbe Release", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        foreach (UploadItem uploadItem in this.UploadItems)
        {
            uploadItem.Validate(this.TryRunScript);
        }

        this.PreflightCheckPassed = this.UploadItems.All(x => !x.HasErrors);
    }

    private ValidationResult TryRunScript(string? script)
    {
        try
        {
            return ValidationResult.Success!;
        }
        catch (Exception e)
        {
            return new ValidationResult(e.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(PreflightCheckPassed))]
    public void Upload()
    {
    }

    #endregion
}