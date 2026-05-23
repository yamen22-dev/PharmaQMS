
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaQMS.API.DTOs.QualityControl;
using PharmaQMS.API.Models.DTOs.Common;
using PharmaQMS.Application.QualityControl;

namespace PharmaQMS.API.Controllers;

[ApiController]
[Route("api/v1/qc-tests")]
[Authorize]
[AllowAnonymous]
public sealed class QcTestController(IQcTestService service) : ControllerBase
{
    [Authorize(Roles = "QCAnalyst, QAManager")]
    [HttpPost]
    [ProducesResponseType(typeof(QcTestSummaryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateQcTestRequest request,
        CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await service.CreateAsync(request, userId, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById),
                new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(QcTestSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<QcTestSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] QcTestListQuery query,
        CancellationToken ct)
    {
        var result = await service.GetAllAsync(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest();
    }
}