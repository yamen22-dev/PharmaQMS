namespace PharmaQMS.API.Core;

public static class RoleNames
{
    public const string QAManager = "QAManager";
    public const string QCAnalyst = "QCAnalyst";
    public const string ProductionAnalyst = "ProductionAnalyst";
    public const string WarehouseOperator = "WarehouseOperator";
    public const string Viewer = "Viewer";

    public static readonly string[] All =
    [
        QAManager,
        QCAnalyst,
        ProductionAnalyst,
        WarehouseOperator,
        Viewer
    ];
}
