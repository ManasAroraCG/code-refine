namespace CodeRefine.Api.Exceptions;

/// <summary>
/// Base class for exceptions that map to a well-defined HTTP status code and a
/// client-safe message. Messages on these exceptions are returned to callers,
/// so they must never contain secrets or internal details.
/// </summary>
public abstract class CodeRefineException : Exception
{
    protected CodeRefineException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    public abstract int StatusCode { get; }

    public abstract string Title { get; }
}

public class NotFoundException : CodeRefineException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status404NotFound;

    public override string Title => "Resource not found";
}

public class ValidationException : CodeRefineException
{
    public ValidationException(string message) : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status400BadRequest;

    public override string Title => "Invalid request";
}

/// <summary>
/// Thrown when an operation is not allowed in the current analysis state, for
/// example creating an improvement pull request before human approval.
/// </summary>
public class InvalidStateException : CodeRefineException
{
    public InvalidStateException(string message) : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status409Conflict;

    public override string Title => "Operation not allowed in current state";
}

public class GitHubApiException : CodeRefineException
{
    public GitHubApiException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    public override int StatusCode => StatusCodes.Status502BadGateway;

    public override string Title => "GitHub request failed";
}

public class AiServiceException : CodeRefineException
{
    public AiServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    public override int StatusCode => StatusCodes.Status502BadGateway;

    public override string Title => "Analysis service request failed";
}

public class GitOperationException : CodeRefineException
{
    public GitOperationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    public override int StatusCode => StatusCodes.Status500InternalServerError;

    public override string Title => "Git operation failed";
}
