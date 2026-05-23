namespace PharmaQMS.API.Models.Entities;

public class BmrLotLink
{
    public Guid Id { get; set; }
    public Guid BmrId { get; set; }
    public int LotId { get; set; }  // int, matching Lot.Id

    public Bmr? Bmr { get; set; }
    public Lot? Lot { get; set; }
}