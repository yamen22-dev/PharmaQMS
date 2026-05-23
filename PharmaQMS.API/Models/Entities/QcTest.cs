using PharmaQMS.API.Models.Enums;

namespace PharmaQMS.API.Models.Entities;

public class QcTest
{
    public int Id { get; set; }

    public required string TestObjectType { get; set; }

    public int TestObjectId { get; set; }

    public QcTestStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public required string CreatedBy { get; set; }

    public ICollection<QcTestParameter> Parameters { get; set; } = [];
}