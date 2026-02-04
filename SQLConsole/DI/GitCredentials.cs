namespace Recom.SQLConsole.DI;

public enum GitAuthMethod
{
    None,
    UsernamePassword,
    SshAgent,
    SshPrivateKey
}

public class GitCredentials
{
    public GitAuthMethod Method { get; set; } = GitAuthMethod.None;

    // For Username/Password
    public string? Username { get; set; }
    public string? Password { get; set; }

    // For SSH
    public string? SshPrivateKeyPath { get; set; }
    public string? SshPassphrase { get; set; }

    // Optional: allow specifying username for ssh
    public string? SshUsername { get; set; }

    // If true, attempt to use SSH agent
    public bool UseAgent { get; set; }
}
