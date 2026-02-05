namespace Recom.SQLConsole.Services;

/// <summary>
/// Provides common Git operations (clone, add, commit, push, pull, status, init).
/// Implementations encapsulate access to a local Git repository and communication with remotes.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Reads the signature from the git config file for further use.
    /// </summary>
    /// <param name="repositoryPath"></param>
    void UseSignatureFromConfig(string repositoryPath);

    /// <summary>
    /// Sets the signature to be used for commits.
    /// </summary>
    /// <param name="name">Name of the author and committer.</param>
    /// <param name="email">EMail of the author and committer.</param>
    void UseSignature(string name, string email);

    /// <summary>
    /// Clone a remote repository into a local target directory.
    /// </summary>
    /// <param name="sourceUrl">URL or path of the source repository (e.g. "https://..." or local path).</param>
    /// <param name="targetPath">Local target path where the repository will be cloned.</param>
    /// <param name="credentials">Optional credentials (username/password). If null, the default credential provider is used.</param>
    /// <param name="branch">Optional branch name to checkout after cloning.</param>
    /// <param name="ct">Cancellation token to support operation cancellation.</param>
    Task CloneAsync(string sourceUrl, string targetPath, GitCredentials? credentials = null, string? branch = null, CancellationToken ct = default);

    /// <summary>
    /// Stage files in the specified repository.
    /// </summary>
    /// <param name="repositoryPath">Path to the local Git repository.</param>
    /// <param name="filePaths">Collection of relative or absolute file paths to stage.</param>
    /// <param name="ct">Cancellation token to support operation cancellation.</param>
    Task AddFilesAsync(string repositoryPath, IEnumerable<string> filePaths, CancellationToken ct = default);

    /// <summary>
    /// Create a commit in the specified repository using the provided author information.
    /// </summary>
    /// <param name="repositoryPath">Path to the local Git repository.</param>
    /// <param name="message">Commit message.</param>
    /// <param name="ct">Cancellation token to support operation cancellation.</param>
    /// <returns>The SHA of the created commit, or null if there were no changes to commit.</returns>
    /// <remarks>
    /// Author information is read from the git config file by default.
    /// Override author if needed using <see cref="UseSignature"/>.
    /// </remarks>
    Task<string?> CommitAsync(string repositoryPath, string message, CancellationToken ct = default);

    /// <summary>
    /// Push local commits to a remote.
    /// </summary>
    /// <param name="repositoryPath">Path to the local Git repository.</param>
    /// <param name="credentials">Optional credentials for remote access.</param>
    /// <param name="remoteName">Name of the remote (default: "origin").</param>
    /// <param name="branchName">Optional branch name to push (default: current HEAD).</param>
    /// <param name="ct">Cancellation token to support operation cancellation.</param>
    /// <returns>Information about the push result (success, error message, rejected refs).</returns>
    Task<PushResultInfo> PushAsync(string repositoryPath, GitCredentials? credentials = null, string remoteName = "origin", string? branchName = null, CancellationToken ct = default);

    /// <summary>
    /// Fetch changes from the remote and merge them into the current branch.
    /// </summary>
    /// <param name="repositoryPath">Path to the local Git repository.</param>
    /// <param name="credentials">Optional credentials for remote access.</param>
    /// <param name="remoteName">Name of the remote (default: "origin").</param>
    /// <param name="branchName">Optional branch name to pull (default: current HEAD).</param>
    /// <param name="ct">Cancellation token to support operation cancellation.</param>
    /// <returns>Information about the pull result (success, merge conflicts, etc.).</returns>
    Task<PullResultInfo> PullAsync(string repositoryPath, GitCredentials? credentials = null, string remoteName = "origin", string? branchName = null, CancellationToken ct = default);

    /// <summary>
    /// Check out the specified branch. If the local branch exists, switch to it.
    /// If it does not exist and a remote branch exists, create a local branch from the remote branch tip and check it out.
    /// If <paramref name="createLocalIfMissing"/> is true, a new local branch will be created from the current HEAD when no remote branch exists.
    /// </summary>
    /// <param name="repositoryPath">Path to the local Git repository.</param>
    /// <param name="branchName">Branch name to check out.</param>
    /// <param name="remoteName">Remote name to look for a remote branch (default: "origin").</param>
    /// <param name="createLocalIfMissing">If true, create a local branch from HEAD when neither local nor remote branch exists.</param>
    /// <param name="ct">Cancellation token to support operation cancellation.</param>
    Task CheckoutBranchAsync(string repositoryPath, string branchName, string remoteName = "origin", bool createLocalIfMissing = true, CancellationToken ct = default);

    /// <summary>
    /// Get the current repository status (modified, staged or untracked files).
    /// </summary>
    /// <param name="repositoryPath">Path to the local Git repository.</param>
    /// <param name="ct">Cancellation token to support operation cancellation.</param>
    /// <returns>A collection of file paths that have status changes.</returns>
    Task<IEnumerable<string>> GetStatusAsync(string repositoryPath, CancellationToken ct = default);

    /// <summary>
    /// Initialize a new Git repository at the given path.
    /// </summary>
    /// <param name="path">Target path for the new repository.</param>
    /// <param name="bare">Whether to create a bare repository.</param>
    /// <param name="ct">Cancellation token to support operation cancellation.</param>
    Task InitializeRepositoryAsync(string path, bool bare = false, CancellationToken ct = default);
}
