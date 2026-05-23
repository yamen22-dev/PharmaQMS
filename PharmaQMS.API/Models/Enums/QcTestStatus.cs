namespace PharmaQMS.API.Models.Enums;

/// <summary>
/// Status lifecycle van een QC-test.
/// Byte-sized voor DOD/storage efficiency.
/// </summary>
public enum QcTestStatus : byte
{
    InBehandeling = 0,
    InProgress = 1,
    Approved = 2,
    Rejected = 3,
}