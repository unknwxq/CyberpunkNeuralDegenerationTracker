
using System.Text.Json;
using SynapseSec.Models;
using SynapseSec.Pipeline;
using SynapseSec.Policies;

var sessions = SampleData.CreateSessions();

Console.WriteLine("SynapseSec: Neural Risk Tracker");
Console.WriteLine();

// --- Assignment 5 ---
Console.WriteLine("Assignment 5: Risk Policy Evaluation\n");
PrintReport("Safe-Net", RiskPipeline.Run(sessions, RiskPolicies.SafeNet));
PrintReport("Overclock", RiskPipeline.Run(sessions, RiskPolicies.Overclock));

// --- Assignment 6 ---
Console.WriteLine("Assignment 6: Workflow Validation\n");
var workflow = new RiskAssessmentWorkflow();

PrintStep("1. Call Before Prepare", workflow.Assess(s => 10 * s.StressLevel));

if (workflow.Prepare(sessions[0]) is SessionValidationResult.Valid { Session: var validSession })
{
    Console.WriteLine($"[2. Validated] {validSession.OperatorId} ({validSession.OperatorName}) | Implant: {validSession.ImplantModel}");
    PrintStep("3. Valid Assessment", workflow.Assess(s => RiskPolicies.SafeNet(s.ToRaw())));
    PrintStep("4. Repeated Call", workflow.Assess(s => 10 * s.StressLevel));
}

// --- Assignment 7 ---
Console.WriteLine("\nAssignment 7: Error Handling\n");

// 1. Normal success and zero risk
PrintDiagnosis("Normal Success (Lucy)", RiskPipeline.DiagnoseOperator(sessions, "NC-02", RiskPolicies.SafeNet));
PrintDiagnosis("Zero-Value Lookup (Kiwi = 0 risk)", RiskPipeline.DiagnoseOperator(sessions, "NC-06", RiskPolicies.SafeNet));

// 2. Domain errors and missing operator
PrintDiagnosis("Domain Error 1 (MentalBreakdown)", RiskPipeline.DiagnoseOperator(sessions, "NC-03", RiskPolicies.Overclock));

var uncertifiedList = new[]
{
    new NeuralSession { OperatorId = "NC-08", OperatorName = "Adam Smasher", ImplantModel = "Borg-Frame-9000 (Illegal)", StressLevel = 5, SessionDurationHours = 2, NeuralLoadGhz = 4 }
};
PrintDiagnosis("Domain Error 2 (ImplantNotRecognized)", RiskPipeline.DiagnoseOperator(uncertifiedList, "NC-08", RiskPolicies.SafeNet));

var corruptedList = new[]
{
    new NeuralSession { OperatorId = "NC-09", OperatorName = "Johnny Silverhand", ImplantModel = "Arasaka Mk.IV (Corporate)", StressLevel = 15, SessionDurationHours = -2, NeuralLoadGhz = 5 }
};
PrintDiagnosis("Domain Error 3 (BrainwaveCorrupted)", RiskPipeline.DiagnoseOperator(corruptedList, "NC-09", RiskPolicies.SafeNet));
PrintDiagnosis("Missing Lookup (OperatorNotFound)", RiskPipeline.DiagnoseOperator(sessions, "NC-404", RiskPolicies.SafeNet));

// 3. File loading tests
string tempDir = Path.Combine(Path.GetTempPath(), "SynapseSecDemo");
Directory.CreateDirectory(tempDir);
string validJsonPath = Path.Combine(tempDir, "sessions.json");
string brokenJsonPath = Path.Combine(tempDir, "corrupted.json");

File.WriteAllText(validJsonPath, JsonSerializer.Serialize(sessions));
File.WriteAllText(brokenJsonPath, "{ \"invalid_json\": [ unclosed ");

PrintFileOutcome("Valid JSON", RiskPipeline.LoadSessionsFromJson(validJsonPath));
PrintFileOutcome("Missing File", RiskPipeline.LoadSessionsFromJson(Path.Combine(tempDir, "missing.json")));
PrintFileOutcome("Malformed JSON", RiskPipeline.LoadSessionsFromJson(brokenJsonPath));

Directory.Delete(tempDir, recursive: true);

// helper methods for printing results
static void PrintReport(string policyName, IReadOnlyList<RiskAssessment> report)
{
    Console.WriteLine($"Policy: {policyName}");
    foreach (var r in report)
    {
        string status = RiskPolicies.ClassifyStatus(r.RiskScore);
        Console.WriteLine($"  {r.SessionId} | {r.OperatorName,-17} | {r.ImplantModel,-37} | Risk: {r.RiskScore,5:F1} -> {status}");
        Console.WriteLine($"        Reason: {r.Reason}");
    }
    Console.WriteLine();
}

static void PrintStep(string label, WorkflowOperationResult result) =>
    Console.WriteLine(result switch
    {
        WorkflowOperationResult.Success { Assessment: var a } =>
            $"[{label}] {a.SessionId} ({a.OperatorName}) -> Risk = {a.RiskScore:F1} ({a.Reason})",
        WorkflowOperationResult.Rejected { Reason: var r } =>
            $"[{label}] Rejected: {r}",
        _ => throw new InvalidOperationException("Unknown workflow result.")
    });

static void PrintDiagnosis(string label, Result<OperatorStatus, NeuralError> result) =>
    Console.WriteLine(result switch
    {
        Result<OperatorStatus, NeuralError>.Success { Value: var s } =>
            $"  [SUCCESS - {label}] {s.OperatorId} ({s.OperatorName}) | Risk: {s.RiskScore:F1} | {s.Status} | {s.Reason}",
        Result<OperatorStatus, NeuralError>.Failure { Error: var e } =>
            $"  [FAILURE - {label}] [{e.Code}] {e.Message}",
        _ => throw new InvalidOperationException("Unknown result.")
    });

static void PrintFileOutcome(string label, Result<IReadOnlyList<NeuralSession>, NeuralError> result) =>
    Console.WriteLine(result switch
    {
        Result<IReadOnlyList<NeuralSession>, NeuralError>.Success { Value: var list } =>
            $"  [FILE OK - {label}] Loaded {list.Count} sessions safely.",
        Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure { Error: var e } =>
            $"  [FILE ERROR - {label}] [{e.Code}] {e.Message}",
        _ => throw new InvalidOperationException("Unknown file result.")
    });
