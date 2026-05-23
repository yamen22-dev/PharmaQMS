namespace PharmaQMS.API.Models.Entities;

public class QcTestParameter
{
    public int Id { get; set; }

    public int QcTestId { get; set; }

    public required string Name { get; set; }

    public required string Unit { get; set; }

    public decimal Min { get; set; }

    public decimal Max { get; set; }

    public decimal? MeasuredValue { get; set; }

    public QcTest? QcTest { get; set; }
}