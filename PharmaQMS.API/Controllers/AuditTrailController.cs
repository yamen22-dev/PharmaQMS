using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaQMS.API.Services.Interfaces;

namespace PharmaQMS.API.Controllers;

[ApiController]
[Route("api/v1/audit-trail")]
[Authorize(Roles = "QAManager,Viewer")]
public sealed class AuditTrailController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PharmaQMS.API.Models.DTOs.Audit.AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var entries = await auditService.GetAllAsync(ct);
        return Ok(entries);
    }
}