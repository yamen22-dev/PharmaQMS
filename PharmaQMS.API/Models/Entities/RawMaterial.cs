using System.ComponentModel.DataAnnotations;

namespace PharmaQMS.API.Models.Entities;

public class RawMaterial
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string PharmaceuticalApi { get; set; } = string.Empty;

    public RawMaterialCategory Category { get; set; }

    [Required]
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    public decimal MinSpecificationLimit { get; set; }

    public decimal MaxSpecificationLimit { get; set; }

    [MaxLength(200)]
    public string Supplier { get; set; } = string.Empty;

    [MaxLength(50)]
    public string CepNumber { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Lot> Lots { get; set; } = new List<Lot>();
}