using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Recom.SQLConsole.Services;

namespace SQLConsole.IntegrationTests;

public class GitServiceIntegrationTests : IDisposable
{
    private readonly string _localTemp;
    private readonly IGitService _gitService;

    public GitServiceIntegrationTests()
    {
        _localTemp = Path.Combine(Path.GetTempPath(), "sqlconsole_git_integration_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_localTemp);

        _gitService = new GitService();
        _gitService.UseSignature("Tester", "tester@example.local");
    }

    [Fact]
    public async Task Clone_Add_Commit_Push_Pull_Workflow()
    {
        string remoteBare = Path.Combine(_localTemp, "remote.git");
        string cloneA = Path.Combine(_localTemp, "cloneA");
        string cloneB = Path.Combine(_localTemp, "cloneB");

        // 1) Init bare remote
        await _gitService.InitializeRepositoryAsync(remoteBare, bare: true);

        // 2) CloneA aus remote
        await _gitService.CloneAsync(remoteBare, cloneA);

        // 3) Create a file, add, commit
        string filePath = Path.Combine(cloneA, "README.md");
        await File.WriteAllTextAsync(filePath, "Hello from cloneA\n");
        await _gitService.AddFilesAsync(cloneA, ["README.md"]);
        string? sha = await _gitService.CommitAsync(cloneA, "Add README");
        Assert.False(string.IsNullOrEmpty(sha));

        // 4) Push to remote
        PushResultInfo pushResult = await _gitService.PushAsync(cloneA);
        Assert.True(pushResult.Success, pushResult.Message ?? "Push failed");

        // 5) CloneB and pull
        await _gitService.CloneAsync(remoteBare, cloneB);

        // Initially no local modifications in cloneB
        List<string> statusB = (await _gitService.GetStatusAsync(cloneB)).ToList();
        Assert.Empty(statusB);

        string readmeB = Path.Combine(cloneB, "README.md");
        Assert.True(File.Exists(readmeB));
        string content = await File.ReadAllTextAsync(readmeB);
        Assert.Contains("Hello from cloneA", content);
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
            // ignore cleanup errors
        }
    }
}
