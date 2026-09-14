using CodeRefine.Api.DTOs.GitHub;
using CodeRefine.Api.Services.GitHub;
using Microsoft.AspNetCore.Mvc;

namespace CodeRefine.Api.Controllers;

[ApiController]
[Route("api/repositories")]
[Produces("application/json")]
public class RepositoriesController : ControllerBase
{
    private readonly IGitHubService _gitHubService;

    public RepositoriesController(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    /// <summary>Lists repositories available to the CodeRefine GitHub App installation.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RepositoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RepositoryDto>>> GetRepositories(CancellationToken cancellationToken)
        => Ok(await _gitHubService.GetRepositoriesAsync(cancellationToken));

    /// <summary>Lists branches for a repository, identified by its internal CodeRefine id.</summary>
    [HttpGet("{id:guid}/branches")]
    [ProducesResponseType(typeof(IReadOnlyList<BranchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> GetBranches(Guid id, CancellationToken cancellationToken)
        => Ok(await _gitHubService.GetBranchesAsync(id, cancellationToken));
}
