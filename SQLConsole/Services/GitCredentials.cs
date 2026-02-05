namespace Recom.SQLConsole.Services;

public enum GitAuthMethod
{
    None,
    UsernamePassword,
    CredentialManager
}

public class GitCredentials
{
    public GitAuthMethod Method { get; set; } = GitAuthMethod.None;

    // For Username/Password
    public string? Username { get; set; }
    public string? Password { get; set; }
}
