using System;
using System.IO;
using System.Threading.Tasks;
using LibGit2Sharp;
using Xunit;
using Recom.SQLConsole.Services;

namespace SQLConsole.IntegrationTests;

public class CheckoutBranchTests : IDisposable
{
    private readonly string _localTemp;
    private readonly IGitService _gitService;

    public CheckoutBranchTests()
    {
        _localTemp = Path.Combine(Path.GetTempPath(), "sqlconsole_git_integration_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_localTemp);

        _gitService = new GitService();
        _gitService.UseSignature("Tester", "tester@example.local");
    }

    [Fact]
    public async Task Checkout_LocalBranch_Succeeds()
    {
        string repoPath = Path.Combine(_localTemp, "localrepo");
        await _gitService.InitializeRepositoryAsync(repoPath, bare: false);

        // Make initial commit so branch operations work
        string readme = Path.Combine(repoPath, "README.md");
        await File.WriteAllTextAsync(readme, "initial\n");
        await _gitService.AddFilesAsync(repoPath, new[] { "README.md" });
        string? sha = await _gitService.CommitAsync(repoPath, "Initial commit");
        Assert.False(string.IsNullOrEmpty(sha));

        // Create local branch using libgit2 via GitService (create by checking out with createLocalIfMissing)
        string branchName = "feature/local";
        await _gitService.CheckoutBranchAsync(repoPath, branchName, createLocalIfMissing: true);

        // verify HEAD is on branch
        using var repo = new Repository(repoPath);
        Assert.Equal(branchName, repo.Head.FriendlyName);
    }

    [Fact]
    public async Task Checkout_RemoteBranch_CreatesLocalFromRemote()
    {
        string remoteBare = Path.Combine(_localTemp, "remote.git");
        string cloneA = Path.Combine(_localTemp, "cloneA");
        string cloneB = Path.Combine(_localTemp, "cloneB");

        // Init bare remote
        await _gitService.InitializeRepositoryAsync(remoteBare, bare: true);

        // CloneA, create a branch and push it to remote
        await _gitService.CloneAsync(remoteBare, cloneA);
        string fileA = Path.Combine(cloneA, "file.txt");
        await File.WriteAllTextAsync(fileA, "hello\n");
        await _gitService.AddFilesAsync(cloneA, new[] { "file.txt" });
        string? sha = await _gitService.CommitAsync(cloneA, "Add file");
        Assert.False(string.IsNullOrEmpty(sha));

        // create branch locally and push
        string branchName = "feature/remote";
        using (var repo = new Repository(cloneA))
        {
            repo.CreateBranch(branchName);
            Commands.Checkout(repo, repo.Branches[branchName]);
        }

        PushResultInfo pushResult = await _gitService.PushAsync(cloneA, null, "origin", branchName);
        Assert.True(pushResult.Success, pushResult.Message ?? "push failed");

        // CloneB from remote (doesn't have a local branch)
        await _gitService.CloneAsync(remoteBare, cloneB);

        // Now check out remote branch via service
        await _gitService.CheckoutBranchAsync(cloneB, branchName, remoteName: "origin", createLocalIfMissing: false);

        using (var repoB = new Repository(cloneB))
        {
            Assert.Equal(branchName, repoB.Head.FriendlyName);
        }
    }

    [Fact]
    public async Task Checkout_MissingBranch_CreateLocalIfRequested()
    {
        string repoPath = Path.Combine(_localTemp, "missingrepo");
        await _gitService.InitializeRepositoryAsync(repoPath, bare: false);

        string readme = Path.Combine(repoPath, "README.md");
        await File.WriteAllTextAsync(readme, "init\n");
        await _gitService.AddFilesAsync(repoPath, new[] { "README.md" });
        string? sha = await _gitService.CommitAsync(repoPath, "Initial");
        Assert.False(string.IsNullOrEmpty(sha));

        string branchName = "new/local/branch";
        await _gitService.CheckoutBranchAsync(repoPath, branchName, createLocalIfMissing: true);

        using var repo = new Repository(repoPath);
        Assert.Equal(branchName, repo.Head.FriendlyName);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_localTemp))
            {
                Directory.Delete(_localTemp, true);
            }
        }
        catch
        {
            // ignore
        }
    }
}