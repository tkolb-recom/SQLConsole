using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using ICSharpCode.AvalonEdit.Document;
using Recom.SQLConsole.Database;
using Recom.SQLConsole.Properties;
using Recom.SQLConsole.Services;

namespace Recom.SQLConsole.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string FilterStringConst = "SQL Dateien (*.sql)|*.sql|Alle Dateien (*.*)|*.*";

    private readonly INavigationService _navigationService;
    private readonly DatabaseService _databaseService;
    private readonly IGitService _gitService;

    public MainViewModel(
        INavigationService navigationService,
        DatabaseService databaseService,
        IGitService gitService)
    {
        _navigationService = navigationService;
        _databaseService = databaseService;
        _gitService = gitService;

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
        this.OnPropertyChanged(nameof(SelectedDatabaseConfig));
        this.OnPropertyChanged(nameof(SelectedFont));
        this.OnPropertyChanged(nameof(SelectedFontSize));
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
    } = null!;

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

        if (_navigationService.ShowDialog(settingsViewModel, this).GetValueOrDefault())
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

    private ObservableCollection<DatabaseConfigViewModel> Databases { get; } = new ObservableCollection<DatabaseConfigViewModel>();

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
    [NotifyCanExecuteChangedFor(nameof(DisconnectDatabaseCommand), nameof(RunScriptCommand), nameof(PreflightCheckCommand))]
    private bool _isRunningScript;

    private bool HasActiveDatabase => _databaseService.ActiveDatabase != null;

    [RelayCommand(CanExecute = nameof(CanConnectDatabase))]
    public void ConnectDatabase()
    {
        try
        {
            _databaseService.ConnectToDatabase(this.SelectedDatabaseConfig!.Settings!, this.SelectedReleaseConfig!.DatabaseName!);
            this.ActiveDatabase = _databaseService.ActiveDatabase;
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Fehler beim Verbinden", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public bool CanConnectDatabase => this.SelectedDatabaseConfig != null && !this.HasActiveDatabase;

    [RelayCommand(CanExecute = nameof(CanDisconnectDatabase))]
    private void DisconnectDatabase()
    {
        if (this.ActiveDatabase == null)
        {
            return;
        }

        try
        {
            _databaseService.DisconnectFromDatabase();
            this.ActiveDatabase = null;
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Fehler beim Trennen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public bool CanDisconnectDatabase => this.HasActiveDatabase && !this.IsRunningScript;

    [RelayCommand(CanExecute = nameof(CanRunScript))]
    public async Task RunScriptAsync(CancellationToken ct = default)
    {
        if (this.ActiveDatabase == null || this.QueryDocument == null)
        {
            return;
        }

        try
        {
            await this.ExecuteStatementAsync();
        }
        catch (Exception e)
        {
            MessageBox.Show(e.ToString(), "Fehler beim Ausführen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExecuteStatementAsync()
    {
        string sql = this.QueryDocument!.Text;
        SqlDatabase db = this.ActiveDatabase!;

        await Task.Run(ExecuteStatement).ContinueWith(DisplayResult, TaskScheduler.FromCurrentSynchronizationContext());

        return;

        (DataTable? data, int? affected) ExecuteStatement()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => this.IsRunningScript = true);

                _databaseService.ExecuteSql(sql, this.StartTransaction, this.CommitTransaction);

                return (data: _databaseService.LastData, affected: _databaseService.LastAffectedRows);
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

                _navigationService.Navigate(resultViewModel, this);
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
    [NotifyCanExecuteChangedFor(nameof(UploadCommand))]
    private bool _preflightCheckPassed;

    public static string SqlFilterString => FilterStringConst;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UploadCommand))]
    private string? _uploadCommitMessage;

    public ObservableCollection<UploadItem> UploadItems { get; } = new ObservableCollection<UploadItem>();

    [RelayCommand]
    public void AddUploadItem()
    {
        var uploadItem = new UploadItem
        {
            // select the first release not already in the list
            Release = this.Releases.FirstOrDefault(
                x => !this.UploadItems.Select<UploadItem, ReleaseConfigViewModel?>(i => i.Release).Contains(x)),
            // preselect the opened file
            FilePath = this.QueryDocument?.FileName ?? string.Empty
        };
        this.UploadItems.Add(uploadItem);
        uploadItem.Validate();

        if (this.UploadItems.Count == 1)
        {
            string file = Path.GetFileName(uploadItem.FilePath);
            this.UploadCommitMessage = $"Upload {file}";
        }

        this.PreflightCheckPassed = false;
        this.PreflightCheckCommand.NotifyCanExecuteChanged();
        this.UploadCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    public void RemoveUploadItem(UploadItem item)
    {
        this.UploadItems.Remove(item);
        this.PreflightCheckCommand.NotifyCanExecuteChanged();
        this.UploadCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanExecutePreflightCheck))]
    public async Task PreflightCheck()
    {
        this.PreflightCheckPassed = false;

        try
        {
            await this.ExecuteValidationAsync();
        }
        catch (Exception e)
        {
            MessageBox.Show(e.ToString(), "Fehler beim Ausführen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public bool CanExecutePreflightCheck => this.UploadItems.Any() && !this.IsRunningScript;

    private async Task ExecuteValidationAsync()
    {
        await Task.Run(Validate).ContinueWith(CheckErrors, TaskScheduler.FromCurrentSynchronizationContext());

        return;

        void Validate()
        {
            foreach (UploadItem uploadItem in this.UploadItems)
            {
                uploadItem.Validate(this.ValidateRelease, this.ValidateUpload);
            }
        }

        void CheckErrors(Task t)
        {
            if (this.UploadItems.Any(x => x.HasErrors))
            {
                MessageBox.Show("Mindestens ein Eintrag ist nicht vollständig gültig.", "Vorflugkontrolle", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(this.UploadCommitMessage))
            {
                MessageBox.Show("Bitte eine Beschreibung für die zu veröffentlichenden Änderungen angeben.", "Vorflugkontrolle", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Alle Prüfungen wurden abgeschlossen.", "Vorflugkontrolle", MessageBoxButton.OK, MessageBoxImage.Information);

            this.PreflightCheckPassed = this.UploadItems.All(x => !x.HasErrors);
        }
    }

    private ValidationResult ValidateRelease(ReleaseConfigViewModel? release)
    {
        if (this.UploadItems.Count(x => Equals(x.Release?.Name, release?.Name)) > 1)
        {
            return new ValidationResult(ValidationMessages.MultipleReferences);
        }

        return ValidationResult.Success!;
    }

    private ValidationResult ValidateUpload(Guid guid)
    {
        try
        {
            UploadItem uploadItem = this.UploadItems.First(x => x.Id == guid);

            string repoPath = Settings.Default.RepositoryPath!;
            string fullPath = Path.GetFullPath(uploadItem.FilePath!);

            if (fullPath.StartsWith(repoPath, StringComparison.InvariantCultureIgnoreCase))
            {
                return new ValidationResult("Die zu veröffentlichende Datei darf nicht bereits im Repository liegen.");
            }

            Application.Current.Dispatcher.Invoke(() => this.IsRunningScript = true);

            return this.ExecuteUploadStatement(uploadItem);
        }
        catch (Exception e)
        {
            return new ValidationResult(e.Message);
        }
        finally
        {
            Application.Current.Dispatcher.Invoke(() => this.IsRunningScript = false);
        }
    }

    private ValidationResult ExecuteUploadStatement(UploadItem uploadItem)
    {
        ReleaseConfigViewModel release = uploadItem.Release!;
        DatabaseConfigViewModel dbConfig
            = this.Databases.First(x => string.Equals(x.Host, release.DatabaseHost, StringComparison.InvariantCultureIgnoreCase));

        try
        {
            string sql = File.ReadAllText(uploadItem.FilePath!);

            _databaseService.ConnectToDatabase(dbConfig.Settings!, release.DatabaseName!);
            _databaseService.ExecuteSql(sql);

            return ValidationResult.Success!;
        }
        catch (Exception e)
        {
            return new ValidationResult(e.Message);
        }
        finally
        {
            _databaseService.DisconnectFromDatabase();
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteUploadCommand))]
    public async Task UploadAsync(CancellationToken ct = default)
    {
        string message = this.UploadCommitMessage!;
        string repo = Settings.Default.RepositoryPath!;
        _gitService.UseSignatureFromConfig(repo);

        GitCredentials credentials = new GitCredentials { Method = GitAuthMethod.CredentialManager };

        try
        {
            foreach (UploadItem uploadItem in this.UploadItems)
            {
                string branch = uploadItem.Release!.RepositoryBranch!;

                string sourceFile = uploadItem.FilePath!;
                string fileName = Path.GetFileName(sourceFile);
                string targetFile = Path.Combine(repo, fileName);

                await _gitService.CheckoutBranchAsync(repo, branch, ct: ct);
                await _gitService.PullAsync(repo, credentials, ct: ct);

                File.Copy(sourceFile, targetFile, true);

                await _gitService.AddFilesAsync(repo, [targetFile], ct: ct);
                await _gitService.CommitAsync(repo, message, ct: ct);
                await _gitService.PushAsync(repo, credentials, ct: ct);
            }
        }
        catch (Exception e)
        {
            MessageBox.Show(e.ToString(), "Fehler beim Hochladen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public bool CanExecuteUploadCommand => !string.IsNullOrWhiteSpace(this.UploadCommitMessage)
                                           && this.PreflightCheckPassed;

    #endregion
}