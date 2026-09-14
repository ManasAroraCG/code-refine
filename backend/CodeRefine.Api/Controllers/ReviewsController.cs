using CodeRefine.Api.Data;
using CodeRefine.Api.DTOs.Analysis;
using CodeRefine.Api.DTOs.GitHub;
using CodeRefine.Api.DTOs.Review;
using CodeRefine.Api.Models;
using CodeRefine.Api.Services.Analysis;
using CodeRefine.Api.Services.GitHub;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeRefine.Api.Controllers;

/// <summary>
/// Human review gate. Approval here is a precondition for any GitHub write.
/// </summary>
[ApiController]
[Route("api/analysis/{id:guid}")]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IGitHubService _gitHubService;
    private readonly IAnalysisService _analysisService;

    public ReviewsController(AppDbContext dbContext, IGitHubService gitHubService, IAnalysisService analysisService)
    {
        _dbContext = dbContext;
        _gitHubService = gitHubService;
        _analysisService = analysisService;
    }

    [HttpPost("approve")]
    [ProducesResponseType(typeof(AnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnalysisResponse>> Approve(
        Guid id,
        [FromBody] ReviewRequest request,
        CancellationToken cancellationToken)
        => Ok(await _analysisService.ApproveAsync(id, request, cancellationToken));

    [HttpPost("reject")]
    [ProducesResponseType(typeof(AnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnalysisResponse>> Reject(
        Guid id,
        [FromBody] ReviewRequest request,
        CancellationToken cancellationToken)
        => Ok(await _analysisService.RejectAsync(id, request, cancellationToken));

    /// <summary>
    /// Creates the improvement pull request. Requires human approval and passing
    /// verification; the backend rejects the request otherwise.
    /// </summary>
    [HttpPost("create-improvement-pr")]
    [ProducesResponseType(typeof(PullRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PullRequestDto>> CreateImprovementPullRequest(
        Guid id,
        [FromBody] ImprovementPrRequest request,
        CancellationToken cancellationToken)
        => Ok(await _analysisService.CreateImprovementPullRequestAsync(id, request, cancellationToken));
}
