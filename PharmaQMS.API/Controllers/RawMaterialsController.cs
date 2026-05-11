using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaQMS.API.Core;
using PharmaQMS.API.DTOs.RawMaterials;
using PharmaQMS.API.Services;
namespace PharmaQMS.API.Controllers;

[ApiController]
[Route("api/v1/raw-materials")]
[Authorize(Roles = RoleNames.QAManager + "," + RoleNames.WarehouseOperator)]
public class RawMaterialsController : ControllerBase
{
    private readonly IRawMaterialService _rawMaterialService;

    public RawMaterialsController(IRawMaterialService rawMaterialService)
    {
        _rawMaterialService = rawMaterialService;
    }
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new { Status = "Raw Materials API is running." });
    }
    [HttpPost]
    [ProducesResponseType(typeof(RawMaterialResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RawMaterialResponse>> Create([FromBody] CreateRawMaterialRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _rawMaterialService.CreateAsync(request, cancellationToken);
            return Created($"/api/v1/raw-materials/{response.Id}", response);
        }
        catch (ArgumentException ex)
        {
            return Problem(title: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(title: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}