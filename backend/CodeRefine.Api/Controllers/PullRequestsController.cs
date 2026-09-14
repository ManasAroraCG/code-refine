using CodeRefine.Api.DTOs.GitHub;
using CodeRefine.Api.Services.GitHub;
using Microsoft.AspNetCore.Mvc;

namespace CodeRefine.Api.Controllers;

/// <summary>
/// Pull request reads for a repository identified by its internal CodeRefine id.
/// </summary>
[ApiController]
[Route("api/repositories/{id:guid}/pull-requests")]
[Produces("application/json")]
public class PullRequestsController : ControllerBase
{
    private readonly IGitHubService _gitHubService;

    public PullRequestsController(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PullRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PullRequestDto>>> GetPullRequests(
        Guid id,
        [FromQuery] string state = "open",
        CancellationToken cancellationToken = default)
        => Ok(await _gitHubService.GetPullRequestsAsync(id, state, cancellationToken));

    [HttpGet("{number:int}")]
    [ProducesResponseType(typeof(PullRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PullRequestDto>> GetPullRequest(Guid id, int number, CancellationToken cancellationToken)
        => Ok(await _gitHubService.GetPullRequestAsync(id, number, cancellationToken));

    [HttpGet("{number:int}/files")]
    [ProducesResponseType(typeof(IReadOnlyList<PullRequestFileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PullRequestFileDto>>> GetPullRequestFiles(Guid id, int number, CancellationToken cancellationToken)
        => Ok(await _gitHubService.GetPullRequestFilesAsync(id, number, cancellationToken));
}
