namespace SupportAI.Repairs;

public interface IRepairAction
{
    string Id { get; }
    string Titulo { get; }
    string Descripcion { get; }
    string Comando { get; }
    bool RequiresElevation => false;
    Task<RepairResult> ExecuteAsync(bool dryRun = false, CancellationToken ct = default);
}

public record RepairResult(bool Success, string Output, string Error = "")
{
    public static RepairResult FromProcess(int exitCode, string stdOut, string stdErr) =>
        new(exitCode == 0, stdOut.Trim(), stdErr.Trim());
}
