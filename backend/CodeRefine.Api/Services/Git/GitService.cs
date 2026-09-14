using System.Diagnostics;

namespace CodeRefine.Api.Services.Git;

public class GitService : IGitService
{
    private readonly ILogger<GitService> _logger;

    public GitService(ILogger<GitService> logger)
    {
        _logger = logger;
    }

    public async Task<string> CloneAsync(string cloneUrl, string? branch = null, CancellationToken cancellationToken = default)
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), "coderefine", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);

        var args = branch is null
            ? $"clone --depth 1 {cloneUrl} ."
            : $"clone --depth 1 --branch {branch} {cloneUrl} .";

        await RunGitAsync(workingDirectory, args, cancellationToken);
        return workingDirectory;
    }

    public Task CheckoutAsync(string workingDirectory, string reference, CancellationToken cancellationToken = default)
        => RunGitAsync(workingDirectory, $"checkout {reference}", cancellationToken);

    public async Task<string> CreateBranchAsync(string workingDirectory, string branchName, CancellationToken cancellationToken = default)
    {
        await RunGitAsync(workingDirectory, $"checkout -b {branchName}", cancellationToken);
        return branchName;
    }

    public async Task<bool> ApplyPatchAsync(string workingDirectory, string diffContent, CancellationToken cancellationToken = default)
    {
        var patchFile = Path.Combine(workingDirectory, $"{Guid.NewGuid():N}.patch");
        await File.WriteAllTextAsync(patchFile, diffContent, cancellationToken);

        try
        {
            await RunGitAsync(workingDirectory, $"apply \"{patchFile}\"", cancellationToken);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to apply patch in {WorkingDirectory}", workingDirectory);
            return false;
        }
        finally
        {
            File.Delete(patchFile);
        }
    }

    public async Task<string> CommitAsync(string workingDirectory, string message, CancellationToken cancellationToken = default)
    {
        await RunGitAsync(workingDirectory, "add -A", cancellationToken);
        await RunGitAsync(workingDirectory, $"commit -m \"{message.Replace("\"", "\\\"")}\"", cancellationToken);
        return await RunGitAsync(workingDirectory, "rev-parse HEAD", cancellationToken);
    }

    public Task PushAsync(string workingDirectory, string branchName, CancellationToken cancellationToken = default)
        => RunGitAsync(workingDirectory, $"push origin {branchName}", cancellationToken);

    public Task<string> GetFileContentAsync(string workingDirectory, string filePath, CancellationToken cancellationToken = default)
        => File.ReadAllTextAsync(Path.Combine(workingDirectory, filePath), cancellationToken);

    public void Cleanup(string workingDirectory)
    {
        try
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to clean up {WorkingDirectory}", workingDirectory);
        }
    }

    private async Task<string> RunGitAsync(string workingDirectory, string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start git {arguments}");

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {arguments} failed: {error}");
        }

        return output.Trim();
    }
}
