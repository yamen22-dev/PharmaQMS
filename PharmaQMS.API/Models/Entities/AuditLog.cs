using System.ComponentModel.DataAnnotations;

namespace PharmaQMS.API.Models.Entities;

public class AuditLog
{
    public long Id { get; set; }

    public DateTime Tijdstip { get; set; }

    [Required]
    [MaxLength(450)]
    public string GebruikerId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Actie { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string EntiteitType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntiteitId { get; set; } = string.Empty;

    public string? OudWaarde { get; set; }

    public string? NieuweWaarde { get; set; }

    [MaxLength(100)]
    public string IPAdres { get; set; } = string.Empty;
}