using System.ComponentModel.DataAnnotations;

namespace PharmaQMS.API.Models.Entities;

public class Lot
{
    public int Id { get; set; }

    public int RawMaterialId { get; set; }

    [Required]
    [MaxLength(100)]
    public string LotNumber { get; set; } = string.Empty;

    public DateTime ReceivedDateUtc { get; set; }

    public decimal Quantity { get; set; }

    public LotStatus Status { get; set; } = LotStatus.Quarantine;

    public RawMaterial? RawMaterial { get; set; }

    public DateTime ExpiryDateUtc { get; set; }

    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string AnalysisCertificate { get; set; } = string.Empty;
}