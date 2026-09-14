using CodeRefine.Api.DTOs.Analysis;
using CodeRefine.Api.Services.Analysis;
using Microsoft.AspNetCore.Mvc;

namespace CodeRefine.Api.Controllers;

[ApiController]
[Route("api/analysis")]
[Produces("application/json")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysisService;

    public AnalysisController(IAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    /// <summary>Starts an analysis run for a pull request or branch.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AnalysisResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnalysisResponse>> StartAnalysis(
        [FromBody] StartAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _analysisService.StartAnalysisAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAnalysis), new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnalysisResponse>> GetAnalysis(Guid id, CancellationToken cancellationToken)
        => Ok(await _analysisService.GetAnalysisAsync(id, cancellationToken));

    [HttpGet("{id:guid}/findings")]
    [ProducesResponseType(typeof(IReadOnlyList<FindingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<FindingDto>>> GetFindings(Guid id, CancellationToken cancellationToken)
        => Ok(await _analysisService.GetFindingsAsync(id, cancellationToken));

    [HttpGet("{id:guid}/patches")]
    [ProducesResponseType(typeof(IReadOnlyList<PatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PatchDto>>> GetPatches(Guid id, CancellationToken cancellationToken)
        => Ok(await _analysisService.GetPatchesAsync(id, cancellationToken));

    [HttpGet("{id:guid}/verification")]
    [ProducesResponseType(typeof(IReadOnlyList<VerificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<VerificationDto>>> GetVerification(Guid id, CancellationToken cancellationToken)
        => Ok(await _analysisService.GetVerificationAsync(id, cancellationToken));
}
