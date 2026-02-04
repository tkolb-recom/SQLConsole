using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Recom.SQLConsole.DI;

namespace SQLConsole.IntegrationTests;

public class GitServiceIntegrationTests : IDisposable
{
    private readonly string _baseTemp;
    private readonly IGitService _gitService;

    public GitServiceIntegrationTests()
    {
        _baseTemp = Path.Combine(Path.GetTempPath(), "sqlconsole_git_integration_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_baseTemp);

        // Verwende die konkrete Implementierung direkt
        _gitService = new GitService();
    }

    [Fact]
    public async Task Clone_Add_Commit_Push_Pull_Workflow()
    {
        var remoteBare = Path.Combine(_baseTemp, "remote.git");
        var cloneA = Path.Combine(_baseTemp, "cloneA");
        var cloneB = Path.Combine(_baseTemp, "cloneB");

        // 1) Init bare remote
        await _gitService.InitializeRepositoryAsync(remoteBare, bare: true);

        // 2) CloneA aus remote
        await _gitService.CloneAsync(remoteBare, cloneA);

        // 3) Create a file, add, commit
        var filePath = Path.Combine(cloneA, "README.md");
        await File.WriteAllTextAsync(filePath, "Hello from cloneA\n");
        await _gitService.AddFilesAsync(cloneA, new[] { "README.md" });
        var sha = await _gitService.CommitAsync(cloneA, "Add README", "Tester", "tester@example.local");
        Assert.False(string.IsNullOrEmpty(sha));

        // 4) Push to remote
        var pushResult = await _gitService.PushAsync(cloneA);
        Assert.True(pushResult.Success, pushResult.Message ?? "Push failed");

        // 5) CloneB and pull
        await _gitService.CloneAsync(remoteBare, cloneB);

        // Initially no local modifications in cloneB
        var statusB = (await _gitService.GetStatusAsync(cloneB)).ToList();
        Assert.Empty(statusB);

        var readmeB = Path.Combine(cloneB, "README.md");
        Assert.True(File.Exists(readmeB));
        var content = await File.ReadAllTextAsync(readmeB);
        Assert.Contains("Hello from cloneA", content);
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
            // ignore cleanup errors
        }
    }
}
