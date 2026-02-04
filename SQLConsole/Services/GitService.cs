namespace Recom.SQLConsole.Services;

public class GitService : IGitService
{
    // Klare, einfache Implementierung mit LibGit2Sharp

    public async Task InitializeRepositoryAsync(string path, bool bare = false, CancellationToken ct = default)
    {
        await Task.Run(Init, ct).ConfigureAwait(false);
        return;

        void Init()
        {
            try
            {
                Repository.Init(path, isBare: bare);
            }
            catch (Exception ex)
            {
                throw new GitServiceException($"Fehler beim Initialisieren des Repositories: {ex.Message}", ex);
            }
        }
    }

    public async Task CloneAsync(
        string sourceUrl, string targetPath, GitCredentials? credentials = null, string? branch = null, CancellationToken ct = default)
    {
        await Task.Run(Clone, ct).ConfigureAwait(false);
        return;

        void Clone()
        {
            try
            {
                var co = new CloneOptions();
                if (!string.IsNullOrEmpty(branch)) co.BranchName = branch;

                co.IsBare = false;
                co.FetchOptions.CredentialsProvider = this.BuildCredentialsHandler(credentials);

                Repository.Clone(sourceUrl, targetPath, co);
            }
            catch (LibGit2Sharp.LibGit2SharpException ex)
            {
                if (ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase)) throw new GitAuthException("Authentifizierungsfehler beim Klonen.", ex);

                throw new GitServiceException("Fehler beim Klonen des Repositories.", ex);
            }
            catch (Exception ex)
            {
                throw new GitServiceException("Fehler beim Klonen des Repositories.", ex);
            }
        }
    }

    public async Task AddFilesAsync(string repositoryPath, IEnumerable<string> filePaths, CancellationToken ct = default)
    {
        await Task.Run(Add, ct).ConfigureAwait(false);
        return;

        void Add()
        {
            try
            {
                if (!Repository.IsValid(repositoryPath)) throw new GitRepositoryNotFoundException($"Kein gültiges Git-Repository unter '{repositoryPath}' gefunden.");

                using var repo = new Repository(repositoryPath);
                Commands.Stage(repo, filePaths);
            }
            catch (Exception ex) when (ex is not GitServiceException)
            {
                throw new GitServiceException("Fehler beim Hinzufügen von Dateien zum Staging-Bereich.", ex);
            }
        }
    }

    public async Task<string?> CommitAsync(
        string repositoryPath, string message, string authorName, string authorEmail, CancellationToken ct = default)
    {
        return await Task.Run(Commit, ct).ConfigureAwait(false);

        string? Commit()
        {
            try
            {
                if (!Repository.IsValid(repositoryPath)) throw new GitRepositoryNotFoundException($"Kein gültiges Git-Repository unter '{repositoryPath}' gefunden.");

                using var repo = new Repository(repositoryPath);
                // Prüfen, ob es Änderungen gibt
                var status = repo.RetrieveStatus(new StatusOptions());
                if (!status.IsDirty) return null; // keine Änderungen zum Committieren

                var author = new Signature(authorName, authorEmail, DateTimeOffset.Now);
                var committer = author;

                var commit = repo.Commit(message, author, committer);
                return commit.Sha;
            }
            catch (Exception ex) when (ex is not GitServiceException)
            {
                throw new GitServiceException("Fehler beim Erstellen eines Commits.", ex);
            }
        }
    }

    public async Task<PushResultInfo> PushAsync(
        string repositoryPath, GitCredentials? credentials = null, string remoteName = "origin", string? branchName = null,
        CancellationToken ct = default)
    {
        return await Task.Run(Push, ct).ConfigureAwait(false);

        PushResultInfo Push()
        {
            try
            {
                if (!Repository.IsValid(repositoryPath)) throw new GitRepositoryNotFoundException($"Kein gültiges Git-Repository unter '{repositoryPath}' gefunden.");

                using var repo = new Repository(repositoryPath);
                var pushOptions = new PushOptions { CredentialsProvider = this.BuildCredentialsHandler(credentials) };

                var remote = repo.Network.Remotes[remoteName] ?? throw new GitServiceException($"Remote '{remoteName}' nicht gefunden.");

                var pushRefSpec = branchName ?? repo.Head.FriendlyName;
                // Erzeuge RefSpec für das Pushen
                var refSpec = $"refs/heads/{pushRefSpec}:refs/heads/{pushRefSpec}";

                var resultInfo = new PushResultInfo { Success = true };

                try
                {
                    repo.Network.Push(remote, refSpec, pushOptions);
                }
                catch (LibGit2Sharp.NonFastForwardException nf)
                {
                    // Remote hat fortgeschrittene commits
                    resultInfo.Success = false;
                    resultInfo.Message = "Push abgelehnt (Non-Fast-Forward). Bitte zuerst pull und merge.";
                    resultInfo.RejectedRefs = new[] { refSpec };
                }

                return resultInfo;
            }
            catch (LibGit2Sharp.LibGit2SharpException ex)
            {
                if (ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase)) throw new GitAuthException("Authentifizierungsfehler beim Push.", ex);

                throw new GitNetworkException("Netzwerkfehler beim Push.", ex);
            }
            catch (Exception ex)
            {
                throw new GitServiceException("Fehler beim Push zum Remote-Repository.", ex);
            }
        }
    }

    public async Task<PullResultInfo> PullAsync(
        string repositoryPath, GitCredentials? credentials = null, string remoteName = "origin", string? branchName = null,
        CancellationToken ct = default)
    {
        return await Task.Run(Pull, ct).ConfigureAwait(false);

        PullResultInfo Pull()
        {
            try
            {
                if (!Repository.IsValid(repositoryPath)) throw new GitRepositoryNotFoundException($"Kein gültiges Git-Repository unter '{repositoryPath}' gefunden.");

                using var repo = new Repository(repositoryPath);

                var remote = repo.Network.Remotes[remoteName] ?? throw new GitServiceException($"Remote '{remoteName}' nicht gefunden.");

                var fetchOptions = new FetchOptions { CredentialsProvider = this.BuildCredentialsHandler(credentials) };

                var branchToPull = branchName ?? repo.Head.FriendlyName;
                // Hole die Daten
                Commands.Fetch(repo, remote.Name, new[] { $"refs/heads/{branchToPull}:refs/remotes/{remote.Name}/{branchToPull}" }, fetchOptions, null);

                // Versuche Merge
                var remoteBranchRef = repo.Branches[$"{remote.Name}/{branchToPull}"];
                if (remoteBranchRef == null) return new PullResultInfo { Success = false, Message = "Remote-Branch nicht gefunden." };

                var mergeResult = repo.Merge(remoteBranchRef, new Signature("SQLConsole", "noreply@sqlconsole.local", DateTimeOffset.Now));

                var pullInfo = new PullResultInfo();
                if (mergeResult.Status == MergeStatus.Conflicts)
                {
                    var conflicts = new List<string>();
                    foreach (var c in repo.Index.Conflicts)
                    {
                        if (c.Ours != null) conflicts.Add(c.Ours.Path);
                        else if (c.Theirs != null) conflicts.Add(c.Theirs.Path);
                        else if (c.Ancestor != null) conflicts.Add(c.Ancestor.Path);
                    }
                    conflicts = conflicts.Distinct().ToList();
                    pullInfo.Success = false;
                    pullInfo.HasConflicts = true;
                    pullInfo.ConflictedFiles = conflicts;
                    pullInfo.Message = "Merge-Konflikte aufgetreten.";

                    throw new GitConflictException("Merge-Konflikte beim Pull.", conflicts);
                }

                pullInfo.Success = true;
                pullInfo.Message = "Pull erfolgreich.";

                return pullInfo;
            }
            catch (LibGit2Sharp.LibGit2SharpException ex)
            {
                if (ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase)) throw new GitAuthException("Authentifizierungsfehler beim Pull.", ex);

                throw new GitNetworkException("Netzwerkfehler beim Pull.", ex);
            }
            catch (GitConflictException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new GitServiceException("Fehler beim Pull vom Remote-Repository.", ex);
            }
        }
    }

    public async Task<IEnumerable<string>> GetStatusAsync(string repositoryPath, CancellationToken ct = default)
    {
        return await Task.Run(Status, ct).ConfigureAwait(false);

        IEnumerable<string> Status()
        {
            try
            {
                if (!Repository.IsValid(repositoryPath)) throw new GitRepositoryNotFoundException($"Kein gültiges Git-Repository unter '{repositoryPath}' gefunden.");

                using var repo = new Repository(repositoryPath);
                var status = repo.RetrieveStatus(new StatusOptions());
                var paths = status.Select(s => s.FilePath).ToList();
                return paths.AsEnumerable();
            }
            catch (Exception ex) when (ex is not GitServiceException)
            {
                throw new GitServiceException("Fehler beim Ermitteln des Repository-Status.", ex);
            }
        }
    }

    public async Task CheckoutBranchAsync(string repositoryPath, string branchName, string remoteName = "origin", bool createLocalIfMissing = true, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branchName))
                    throw new GitServiceException("Branch-Name darf nicht leer sein.");

                if (!Repository.IsValid(repositoryPath))
                    throw new GitRepositoryNotFoundException($"Kein gültiges Git-Repository unter '{repositoryPath}' gefunden.");

                using var repo = new Repository(repositoryPath);

                // Prüfe auf existierenden lokalen Branch
                var local = repo.Branches[branchName];
                if (local != null)
                {
                    Commands.Checkout(repo, local);
                    return;
                }

                // Prüfe auf vorhandenen Remote-Branch
                var remoteBranch = repo.Branches[$"{remoteName}/{branchName}"];
                if (remoteBranch != null)
                {
                    // Erstelle lokalen Branch vom Remote-Tip und checke aus
                    var newBranch = repo.CreateBranch(branchName, remoteBranch.Tip);
                    // Optional: setze Tracking-Informationen (falls unterstützt)
                    try
                    {
                        repo.Branches.Update(newBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
                    }
                    catch
                    {
                        // Ignore: Tracking update ist optional und kann in manchen LibGit2Sharp-Versionen fehlschlagen
                    }

                    Commands.Checkout(repo, newBranch);
                    return;
                }

                if (createLocalIfMissing)
                {
                    var created = repo.CreateBranch(branchName);
                    Commands.Checkout(repo, created);
                    return;
                }

                throw new GitServiceException($"Branch '{branchName}' nicht gefunden (weder lokal noch auf Remote '{remoteName}').");
            }
            catch (LibGit2Sharp.LibGit2SharpException ex)
            {
                if (ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase))
                    throw new GitAuthException("Authentifizierungsfehler beim Wechseln des Branches.", ex);

                throw new GitServiceException("Fehler beim Wechseln des Branches.", ex);
            }
            catch (Exception ex)
            {
                throw new GitServiceException("Fehler beim Wechseln des Branches.", ex);
            }
        }, ct).ConfigureAwait(false);
    }

    private CredentialsHandler BuildCredentialsHandler(GitCredentials? credentials)
    {
        return (url, usernameFromUrl, types) =>
               {
                   if (credentials == null || credentials.Method == GitAuthMethod.None)
                       return null!; // LibGit2Sharp behandelt null als Default-Provider

                   if (credentials.Method == GitAuthMethod.UsernamePassword)
                   {
                       return new UsernamePasswordCredentials
                       {
                           Username = credentials.Username ?? usernameFromUrl,
                           Password = credentials.Password ?? string.Empty
                       };
                   }

                   // SSH currently isn't supported

                   return null!;
               };
    }
}