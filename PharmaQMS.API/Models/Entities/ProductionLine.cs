namespace PharmaQMS.API.Models.Entities;

public class ProductionLine
{
    public Guid Id { get; set; }
    public required string LineName { get; set; }
    public required string Location { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Bmr> Bmrs { get; set; } = [];
}