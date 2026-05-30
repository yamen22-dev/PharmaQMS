namespace PharmaQMS.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaQMS.API.Models.DTOs.Bmr;
using PharmaQMS.API.Models.DTOs.Common;
using PharmaQMS.API.Models.Enums;
using PharmaQMS.API.Services.Interfaces;
using PharmaQMS.API.Services.Interfaces.MasterRecipe;

[ApiController]
[Route("api/v1/bmr")]
[Authorize]
public sealed class BmrController(IBmrService bmrService, IMasterRecipeService _masterRecipeService) : ControllerBase
{
    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found.");

    [HttpPost("new")]
    [Authorize(Roles = "QAManager,ProductionAnalyst")]
    [ProducesResponseType(typeof(BmrDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBmrRequest request,
        CancellationToken ct)
    {
        var result = await bmrService.CreateAsync(request, UserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<BmrSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] BmrStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new BmrListQuery { Status = status, Page = page, PageSize = pageSize };
        var result = await bmrService.GetAllAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BmrDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await bmrService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("{bmrId:guid}/steps/{stepId:guid}/confirm")]
    [Authorize(Roles = "QAManager,ProductionAnalyst")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmStep(
        Guid bmrId,
        Guid stepId,
        [FromBody] ConfirmStepRequest request,
        CancellationToken ct)
    {
        await bmrService.ConfirmStepAsync(bmrId, stepId, request, UserId, ct);
        return NoContent();
  
    }

    [HttpPost("{bmrId:guid}/steps/{stepId:guid}/verify")]
    [Authorize(Roles = "QAManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyStep(
        Guid bmrId,
        Guid stepId,
        [FromBody] VerifyStepRequest request,
        CancellationToken ct)
    {
        await bmrService.VerifyStepAsync(bmrId, stepId, request, UserId, ct);
        return NoContent();
    }

    [HttpGet("master-recipe-approved")]
    public async Task<IActionResult> GetApprovedMasterRecipesAsync(
        [FromQuery] string? status,
        CancellationToken ct = default)
    {
        var result = await _masterRecipeService.GetApprovedMasterRecipesAsync(status, ct);
        return Ok(result);
    }

    [HttpGet("production-lines")]
    public async Task<IActionResult> GetProductionLinesAsync(
        CancellationToken ct = default)
    {
        var result = await bmrService.GetProductionLinesAsync(ct);
        return Ok(result);
    }
}