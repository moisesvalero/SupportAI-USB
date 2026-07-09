namespace SupportAI.Repairs;

public static class RepairCatalog
{
    private static readonly List<IRepairAction> Repairs =
    [
        new DnsFlushRepair(),
        new TempCleanRepair(),
        new ExplorerRestartRepair(),
    ];

    public static IReadOnlyList<IRepairAction> All => Repairs.AsReadOnly();

    public static IRepairAction? Get(string id) =>
        Repairs.FirstOrDefault(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
