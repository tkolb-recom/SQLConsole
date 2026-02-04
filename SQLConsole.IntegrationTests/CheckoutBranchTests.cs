using System;
using System.IO;
using System.Threading.Tasks;
using LibGit2Sharp;
using Xunit;
using Recom.SQLConsole.DI;

namespace SQLConsole.IntegrationTests;

public class CheckoutBranchTests : IDisposable
{
    private readonly string _baseTemp;
    private readonly IGitService _gitService;

    public CheckoutBranchTests()
    {
        _baseTemp = Path.Combine(Path.GetTempPath(), "sqlconsole_git_integration_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_baseTemp);
        _gitService = new GitService();
    }

    [Fact]
    public async Task Checkout_LocalBranch_Succeeds()
    {
        var repoPath = Path.Combine(_baseTemp, "localrepo");
        await _gitService.InitializeRepositoryAsync(repoPath, bare: false);

        // Make initial commit so branch operations work
        var readme = Path.Combine(repoPath, "README.md");
        await File.WriteAllTextAsync(readme, "initial\n");
        await _gitService.AddFilesAsync(repoPath, new[] { "README.md" });
        var sha = await _gitService.CommitAsync(repoPath, "Initial commit", "Tester", "tester@example.local");
        Assert.False(string.IsNullOrEmpty(sha));

        // Create local branch using libgit2 via GitService (create by checking out with createLocalIfMissing)
        var branchName = "feature/local";
        await _gitService.CheckoutBranchAsync(repoPath, branchName, createLocalIfMissing: true);

        // verify HEAD is on branch
        using var repo = new LibGit2Sharp.Repository(repoPath);
        Assert.Equal(branchName, repo.Head.FriendlyName);
    }

    [Fact]
    public async Task Checkout_RemoteBranch_CreatesLocalFromRemote()
    {
        var remoteBare = Path.Combine(_baseTemp, "remote.git");
        var cloneA = Path.Combine(_baseTemp, "cloneA");
        var cloneB = Path.Combine(_baseTemp, "cloneB");

        // Init bare remote
        await _gitService.InitializeRepositoryAsync(remoteBare, bare: true);

        // CloneA, create branch and push it to remote
        await _gitService.CloneAsync(remoteBare, cloneA);
        var fileA = Path.Combine(cloneA, "file.txt");
        await File.WriteAllTextAsync(fileA, "hello\n");
        await _gitService.AddFilesAsync(cloneA, new[] { "file.txt" });
        var sha = await _gitService.CommitAsync(cloneA, "Add file", "Tester", "tester@example.local");
        Assert.False(string.IsNullOrEmpty(sha));

        // create branch locally and push
        var branchName = "feature/remote";
        using (var repo = new LibGit2Sharp.Repository(cloneA))
        {
            repo.CreateBranch(branchName);
            Commands.Checkout(repo, repo.Branches[branchName]);
        }

        var pushResult = await _gitService.PushAsync(cloneA, null, "origin", branchName);
        Assert.True(pushResult.Success, pushResult.Message ?? "push failed");

        // CloneB from remote (doesn't have local branch)
        await _gitService.CloneAsync(remoteBare, cloneB);

        // Now checkout remote branch via service
        await _gitService.CheckoutBranchAsync(cloneB, branchName, remoteName: "origin", createLocalIfMissing: false);

        using (var repoB = new LibGit2Sharp.Repository(cloneB))
        {
            Assert.Equal(branchName, repoB.Head.FriendlyName);
        }
    }

    [Fact]
    public async Task Checkout_MissingBranch_CreateLocalIfRequested()
    {
        var repoPath = Path.Combine(_baseTemp, "missingrepo");
        await _gitService.InitializeRepositoryAsync(repoPath, bare: false);

        var readme = Path.Combine(repoPath, "README.md");
        await File.WriteAllTextAsync(readme, "init\n");
        await _gitService.AddFilesAsync(repoPath, new[] { "README.md" });
        var sha = await _gitService.CommitAsync(repoPath, "Initial", "Tester", "tester@example.local");
        Assert.False(string.IsNullOrEmpty(sha));

        var branchName = "new/local/branch";
        await _gitService.CheckoutBranchAsync(repoPath, branchName, createLocalIfMissing: true);

        using var repo = new LibGit2Sharp.Repository(repoPath);
        Assert.Equal(branchName, repo.Head.FriendlyName);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_baseTemp))
                Directory.Delete(_baseTemp, true);
        }
        catch
        {
            // ignore
        }
    }
}
