namespace CodeRefine.Api.Services.Git;

public interface IGitService
{
    Task<string> CloneAsync(string cloneUrl, string? branch = null, CancellationToken cancellationToken = default);

    Task CheckoutAsync(string workingDirectory, string reference, CancellationToken cancellationToken = default);

    Task<string> CreateBranchAsync(string workingDirectory, string branchName, CancellationToken cancellationToken = default);

    Task<bool> ApplyPatchAsync(string workingDirectory, string diffContent, CancellationToken cancellationToken = default);

    Task<string> CommitAsync(string workingDirectory, string message, CancellationToken cancellationToken = default);

    Task PushAsync(string workingDirectory, string branchName, CancellationToken cancellationToken = default);

    Task<string> GetFileContentAsync(string workingDirectory, string filePath, CancellationToken cancellationToken = default);

    void Cleanup(string workingDirectory);
}
