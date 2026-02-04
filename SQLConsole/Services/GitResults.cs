namespace Recom.SQLConsole.Services;

public class PushResultInfo
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string>? RejectedRefs { get; set; }
}

public class PullResultInfo
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public bool HasConflicts { get; set; }
    public IEnumerable<string>? ConflictedFiles { get; set; }
}
