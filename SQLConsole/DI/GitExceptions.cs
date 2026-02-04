namespace Recom.SQLConsole.DI;

public class GitServiceException : Exception
{
    public GitServiceException(string message, Exception? inner = null) : base(message, inner) { }
}

public class GitAuthException : GitServiceException
{
    public GitAuthException(string message, Exception? inner = null) : base(message, inner) { }
}

public class GitRepositoryNotFoundException : GitServiceException
{
    public GitRepositoryNotFoundException(string message) : base(message) { }
}

public class GitConflictException : GitServiceException
{
    public IEnumerable<string>? ConflictedFiles { get; }

    public GitConflictException(string message, IEnumerable<string>? conflictedFiles = null) : base(message)
    {
        ConflictedFiles = conflictedFiles;
    }
}

public class GitNetworkException : GitServiceException
{
    public GitNetworkException(string message, Exception? inner = null) : base(message, inner) { }
}
