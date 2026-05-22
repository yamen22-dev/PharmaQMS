using Microsoft.EntityFrameworkCore;
using PharmaQMS.API.Data;
using PharmaQMS.API.DTOs.Lot;
using PharmaQMS.API.Models.Entities;

namespace PharmaQMS.API.Services;

public sealed class LotService : ILotService
{
    private readonly DomainDbContext _db;
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;

    // Status transitions allowed per role:
    // QA Manager  : Quarantine → Released | Quarantine → Rejected
    // QC Analyst  : (read-only; no status changes — extend here when scope expands)
    private static readonly IReadOnlyDictionary<LotStatus, LotStatus[]> AllowedTransitions =
        new Dictionary<LotStatus, LotStatus[]>
        {
            [LotStatus.Quarantine] = new[] { LotStatus.Released, LotStatus.Rejected },
            [LotStatus.Released] = Array.Empty<LotStatus>(),   // UC-02g Alt-C: geen verdere overgang
            [LotStatus.Rejected] = Array.Empty<LotStatus>(),   // UC-02g Alt-C: geen verdere overgang
        };

    public LotService(
        DomainDbContext db,
        IAuthService authService,
        IAuditService auditService)
    {
        _db = db;
        _authService = authService;
        _auditService = auditService;
    }

    // ── UC-02e ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<LotSummaryResponse>> GetLotsAsync(
        string? status,
        int? rawMaterialId,
        CancellationToken ct = default)
    {
        var query = _db.Lots
            .AsNoTracking()
            .Include(l => l.RawMaterial)
            .AsQueryable();

        if (rawMaterialId.HasValue)
            query = query.Where(l => l.RawMaterialId == rawMaterialId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<LotStatus>(status, ignoreCase: true, out var parsedStatus))
            query = query.Where(l => l.Status == parsedStatus);

        return await query
            .OrderByDescending(l => l.ReceivedDateUtc)
            .Select(l => new LotSummaryResponse(
                l.Id,
                l.LotNumber,
                l.RawMaterialId,
                l.RawMaterial!.Name,
                l.RawMaterial!.Supplier,
                l.Quantity,
                l.RawMaterial!.Unit,
                l.ReceivedDateUtc,
                l.ExpiryDateUtc,
                l.Status.ToString()))
            .ToListAsync(ct);
    }

    // ── UC-02f ────────────────────────────────────────────────────────────────

    public async Task<LotDetailResponse?> GetLotByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Lots
            .AsNoTracking()
            .Include(l => l.RawMaterial)
            .Where(l => l.Id == id)
            .Select(l => new LotDetailResponse(
                l.Id,
                l.LotNumber,
                l.RawMaterialId,
                l.RawMaterial!.Name,
                l.RawMaterial!.Supplier,
                l.RawMaterial!.PharmaceuticalApi,
                l.Quantity,
                l.RawMaterial!.Unit,
                l.ReceivedDateUtc,
                l.RawMaterial!.MinSpecificationLimit,
                l.RawMaterial!.MaxSpecificationLimit,
                l.Status.ToString(),
                l.ExpiryDateUtc))
            .FirstOrDefaultAsync(ct);
    }

    // ── UC-02d ────────────────────────────────────────────────────────────────

    public async Task<LotDetailResponse> CreateLotAsync(
        int rawMaterialId,
        CreateLotRequest request,
        string createdByUserId,
        CancellationToken ct = default)
    {
        // Precondition: grondstof moet bestaan
        var rawMaterial = await _db.RawMaterials
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rawMaterialId, ct)
            ?? throw new KeyNotFoundException($"Grondstof met id {rawMaterialId} niet gevonden.");

        // Alt-B: vervaldatum via grondstof ligt in het verleden — niet van toepassing
        // (vervaldatum zit op RawMaterial, niet op Lot)
        if (request.ExpiryDateUtc.Date <= DateTime.UtcNow.Date)
            throw new InvalidOperationException(
                "De vervaldatum van de grondstof ligt in het verleden. Er kunnen geen nieuwe lots worden geregistreerd.");

        // Alt-A: dubbel lotnummer per grondstof
        bool duplicate = await _db.Lots
            .AsNoTracking()
            .AnyAsync(l => l.RawMaterialId == rawMaterialId &&
                           l.LotNumber == request.LotNumber, ct);

        if (duplicate)
            throw new InvalidOperationException(
                "Dit lotnummer bestaat al voor de geselecteerde grondstof.");

        var lot = new Lot
        {
            RawMaterialId = rawMaterialId,
            LotNumber = request.LotNumber,
            Quantity = request.Quantity,
            ReceivedDateUtc = request.ReceivedDateUtc.ToUniversalTime(),
            ExpiryDateUtc = request.ExpiryDateUtc.ToUniversalTime(),
            PurchaseOrderNumber = request.PurchaseOrderNumber,
            AnalysisCertificate = request.AnalysisCertificate ?? "",
            Status = LotStatus.Quarantine,   
        };

        _db.Lots.Add(lot);
        await _db.SaveChangesAsync(ct);

        // Audit Trail
        await _auditService.LogAsync(
            entityName: "Lot",
            entityId: lot.Id.ToString(),
            action: "CREATE",
            oldValue: null,
            newValue: $"LotNumber={lot.LotNumber};RawMaterialId={rawMaterialId};Status=Quarantine",
            performedByUserId: createdByUserId,
            ct: ct);

        return new LotDetailResponse(
            lot.Id,
            lot.LotNumber,
            rawMaterial.Id,
            rawMaterial.Name,
            rawMaterial.Supplier,
            rawMaterial.PharmaceuticalApi,
            lot.Quantity,
            rawMaterial.Unit,
            lot.ReceivedDateUtc,
            rawMaterial.MinSpecificationLimit,
            rawMaterial.MaxSpecificationLimit,
            lot.Status.ToString(),
            lot.ExpiryDateUtc);
    }

    // ── UC-02g ────────────────────────────────────────────────────────────────

    public async Task<LotStatusChangedResponse> ChangeLotStatusAsync(
        int id,
        ChangeLotStatusRequest request,
        string changedByUserId,
        CancellationToken ct = default)
    {
        // Alt-B: elektronische handtekening verificeren
        bool signatureValid = await _authService.VerifyPasswordAsync(changedByUserId, request.Password, ct);
        if (!signatureValid)
            throw new UnauthorizedAccessException("Elektronische handtekening incorrect.");

        var lot = await _db.Lots.FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new KeyNotFoundException($"Lot met id {id} niet gevonden.");

        if (!Enum.TryParse<LotStatus>(request.NewStatus, ignoreCase: true, out var newStatus))
            throw new ArgumentException($"Ongeldige status: {request.NewStatus}.");

        // Alt-C: niet-toegestane overgang
        if (!AllowedTransitions[lot.Status].Contains(newStatus))
            throw new InvalidOperationException(
                $"Overgang van {lot.Status} naar {newStatus} is niet toegestaan.");

        var oldStatus = lot.Status;
        lot.Status = newStatus;
        await _db.SaveChangesAsync(ct);

        // Audit Trail — oud en nieuw vastleggen (FR13, NF03)
        await _auditService.LogAsync(
            entityName: "Lot",
            entityId: lot.Id.ToString(),
            action: "STATUS_CHANGE",
            oldValue: oldStatus.ToString(),
            newValue: $"Status={newStatus};Reason={request.Reason}",
            performedByUserId: changedByUserId,
            ct: ct);

        return new LotStatusChangedResponse(lot.Id, lot.LotNumber, oldStatus.ToString(), newStatus.ToString());
    }
}