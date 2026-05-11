using System.ComponentModel.DataAnnotations;
using PharmaQMS.API.Infrastructure;

namespace PharmaQMS.API.DTOs.RawMaterials;

public class CreateRawMaterialRequest
{
    [Required]
    [StringLength(200)]
    [SanitizedString(maxLength: 200, allowHtml: false)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [SanitizedString(maxLength: 200, allowHtml: false)]
    public string PharmaceuticalApi { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [SanitizedString(maxLength: 50, allowHtml: false)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [SanitizedString(maxLength: 50, allowHtml: false)]
    public string Unit { get; set; } = string.Empty;

    [Range(0, 999999999)]
    public decimal MinSpecificationLimit { get; set; }

    [Range(0, 999999999)]
    public decimal MaxSpecificationLimit { get; set; }

    [StringLength(200)]
    [SanitizedString(maxLength: 200, allowHtml: false)]
    public string Supplier { get; set; } = string.Empty;

    [StringLength(50)]
    [SanitizedString(maxLength: 50, allowHtml: false)]
    public string CepNumber { get; set; } = string.Empty;

    [StringLength(1000)]
    [SanitizedString(maxLength: 1000, allowHtml: false)]
    public string Notes { get; set; } = string.Empty;
}