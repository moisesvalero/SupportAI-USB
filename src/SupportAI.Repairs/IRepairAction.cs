namespace SupportAI.Repairs;

public interface IRepairAction
{
    string Id { get; }
    string Titulo { get; }
    string Descripcion { get; }
    string Comando { get; }
    Task<RepairResult> ExecuteAsync(bool dryRun = false);
}

public record RepairResult(bool Success, string Output, string Error = "")
{
    public static RepairResult FromProcess(int exitCode, string stdOut, string stdErr) =>
        new(exitCode == 0, stdOut.Trim(), stdErr.Trim());
}
