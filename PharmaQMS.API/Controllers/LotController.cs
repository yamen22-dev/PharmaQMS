using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaQMS.API.DTOs.Lot;
using PharmaQMS.API.Services;

namespace PharmaQMS.API.Controllers;

[ApiController]
[Route("api/v1/raw-materials/{rawMaterialId:int}/lots")]
[Authorize]
public sealed class LotsController : ControllerBase
{
    private readonly ILotService _lotService;

    public LotsController(ILotService lotService)
    {
        _lotService = lotService;
    }

    // ── UC-02e: Lots overzicht ─────────────────────────────────────────────
    // GET /api/v1/lots  (globaal overzicht, alle rollen)
    [HttpGet("/api/v1/lots")]
    public async Task<ActionResult<IReadOnlyList<LotSummaryResponse>>> GetLots(
        [FromQuery] string? status,
        [FromQuery] int? rawMaterialId,
        CancellationToken ct)
    {
        var lots = await _lotService.GetLotsAsync(status, rawMaterialId, ct);
        return Ok(lots);
    }

    [HttpGet("/api/v1/lots/released")]
    public async Task<ActionResult<IReadOnlyList<LotSummaryResponse>>> GetReleasedLots(
        CancellationToken ct)
    {
        var lots = await _lotService.GetReleasedLotsAsync(ct);
        return Ok(lots);
    }

    // GET /api/v1/raw-materials/{rawMaterialId}/lots  (SPA-compatibel overzicht)
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LotSummaryResponse>>> GetLotsForRawMaterial(
        int rawMaterialId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var lots = await _lotService.GetLotsAsync(status, rawMaterialId, ct);
        return Ok(lots);
    }

    // ── UC-02f: Lot detail ─────────────────────────────────────────────────
    // GET /api/v1/lots/{id}  (alle rollen)
    [HttpGet("/api/v1/lots/{id:int}")]
    public async Task<ActionResult<LotDetailResponse>> GetLot(int id, CancellationToken ct)
    {
        var lot = await _lotService.GetLotByIdAsync(id, ct);
        if (lot is null) return NotFound();
        return Ok(lot);
    }

    // GET /api/v1/raw-materials/{rawMaterialId}/lots/{id}  (SPA-compatibel detail)
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LotDetailResponse>> GetLotForRawMaterial(int rawMaterialId, int id, CancellationToken ct)
    {
        var lot = await _lotService.GetLotByIdAsync(id, ct);
        if (lot is null || lot.RawMaterialId != rawMaterialId) return NotFound();
        return Ok(lot);
    }

    // ── UC-02d: Lot registreren ────────────────────────────────────────────
    // POST /api/v1/raw-materials/{rawMaterialId}/lots
    [HttpPost]
    [Authorize(Roles = "QAManager,WarehouseOperator")]
    public async Task<ActionResult<LotDetailResponse>> CreateLot(
        int rawMaterialId,
        [FromBody] CreateLotRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        string userId = GetUserId();

        try
        {
            var result = await _lotService.CreateLotAsync(rawMaterialId, request, userId, ct);
            return CreatedAtAction(
                nameof(GetLotForRawMaterial),
                routeValues: new { rawMaterialId, id = result.Id },
                value: result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Alt-A: dubbel lotnummer
            return Conflict(new { message = ex.Message });
        }
    }

    // ── UC-02g: Kwaliteitsstatus wijzigen ─────────────────────────────────
    // POST /api/v1/lots/{id}/status
    [HttpPost("/api/v1/lots/{id:int}/status")]
    [Authorize(Roles = "QAManager")]
    public async Task<ActionResult<LotStatusChangedResponse>> ChangeLotStatus(
        int id,
        [FromBody] ChangeLotStatusRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        string userId = GetUserId();

        try
        {
            var result = await _lotService.ChangeLotStatusAsync(id, request, userId, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Alt-B: onjuiste elektronische handtekening — poging wordt gelogd in service
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Alt-C: niet-toegestane overgang
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private string GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Gebruiker-ID niet gevonden in token.");
        return raw;
    }
}