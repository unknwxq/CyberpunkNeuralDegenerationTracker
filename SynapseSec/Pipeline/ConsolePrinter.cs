using System.Text.Json;
using SynapseSec.Models;
using SynapseSec.Native;
using SynapseSec.Policies;

namespace SynapseSec.Pipeline;

// handles all console output so Program.cs stays super clean and short
public static class ConsolePrinter
{
    // runs and prints assignment 5 demo
    public static void RunAssignment5(IReadOnlyList<NeuralSession> sessions)
    {
        Console.WriteLine("ASSIGNMENT 5: Functional Pipeline & Implant Risk Policies");


        PrintPolicyTable("Safe-Net (Corporate Regulations)", RiskPipeline.Run(sessions, RiskPolicies.SafeNet));
        PrintPolicyTable("Overclock (Street Redline Protocol)", RiskPipeline.Run(sessions, RiskPolicies.Overclock));
    }

    // runs and prints assignment 6 demo with clear explanation for human user
    public static void RunAssignment6(IReadOnlyList<NeuralSession> sessions)
    {
        Console.WriteLine("ASSIGNMENT 6: Safe State Machine & Temporal Coupling Protection");
        Console.WriteLine("Goal: Prevent calculating risk before session is validated, and stop double runs.\n");

        var workflow = new RiskAssessmentWorkflow();

        Console.WriteLine($"  [Step 1] Current State: {workflow.State} -> Trying to calculate risk BEFORE validation...");
        PrintStepOutcome("Blocked Premature Call", workflow.Assess(s => 10 * s.StressLevel));

        Console.WriteLine($"\n  [Step 2] Running SessionFactory.Create() for operator {sessions[0].OperatorId} ({sessions[0].OperatorName})...");
        if (workflow.Prepare(sessions[0]) is SessionValidationResult.Valid { Session: var valid })
        {
            Console.WriteLine($"           Success! State changed: NotReady -> {workflow.State}. Chip verified: {valid.ImplantModel}");

            Console.WriteLine($"\n  [Step 3] Current State: {workflow.State} -> Running safe risk calculation now...");
            PrintStepOutcome("Valid Assessment", workflow.Assess(s => RiskPolicies.SafeNet(s.ToRaw())));
            Console.WriteLine($"           State changed: Ready -> {workflow.State}. Result locked in memory.");

            Console.WriteLine($"\n  [Step 4] Current State: {workflow.State} -> Trying to run calculation a SECOND time...");
            PrintStepOutcome("Blocked Repeated Call", workflow.Assess(s => 10 * s.StressLevel));
        }
        Console.WriteLine();
    }

    // runs and prints assignment 7 demo
    public static void RunAssignment7(IReadOnlyList<NeuralSession> sessions)
    {
        Console.WriteLine("ASSIGNMENT 7: Railway Error Handling (Result, LookupResult, Map/Bind & JSON I/O)");

        PrintDiagnosis("Normal Success (Lucy)", RiskPipeline.DiagnoseOperator(sessions, "NC-02", RiskPolicies.SafeNet));
        PrintDiagnosis("Zero-Value Lookup (Kiwi = 0 risk)", RiskPipeline.DiagnoseOperator(sessions, "NC-06", RiskPolicies.SafeNet));
        PrintDiagnosis("Domain Error 1 (MentalBreakdown)", RiskPipeline.DiagnoseOperator(sessions, "NC-03", RiskPolicies.Overclock));

        var illegalChip = new[] { new NeuralSession { OperatorId = "NC-08", OperatorName = "Adam Smasher", ImplantModel = "Borg-Frame-9000 (Illegal)", StressLevel = 5, SessionDurationHours = 2, NeuralLoadGhz = 4 } };
        PrintDiagnosis("Domain Error 2 (ImplantNotRecognized)", RiskPipeline.DiagnoseOperator(illegalChip, "NC-08", RiskPolicies.SafeNet));

        var badMetrics = new[] { new NeuralSession { OperatorId = "NC-09", OperatorName = "Johnny Silverhand", ImplantModel = "Arasaka Mk.IV (Corporate)", StressLevel = 15, SessionDurationHours = -2, NeuralLoadGhz = 5 } };
        PrintDiagnosis("Domain Error 3 (BrainwaveCorrupted)", RiskPipeline.DiagnoseOperator(badMetrics, "NC-09", RiskPolicies.SafeNet));
        PrintDiagnosis("Missing Lookup (OperatorNotFound)", RiskPipeline.DiagnoseOperator(sessions, "NC-404", RiskPolicies.SafeNet));

        string tempDir = Path.Combine(Path.GetTempPath(), "SynapseSecDemo");
        Directory.CreateDirectory(tempDir);
        string validJsonPath = Path.Combine(tempDir, "sessions.json");
        string brokenJsonPath = Path.Combine(tempDir, "corrupted.json");

        File.WriteAllText(validJsonPath, JsonSerializer.Serialize(sessions));
        File.WriteAllText(brokenJsonPath, "{ \"invalid_json\": [ unclosed ");

        PrintFileOutcome("Valid JSON", RiskPipeline.LoadSessionsFromJson(validJsonPath));
        PrintFileOutcome("Missing File", RiskPipeline.LoadSessionsFromJson(Path.Combine(tempDir, "missing.json")));
        PrintFileOutcome("Malformed JSON (Intentional Test)", RiskPipeline.LoadSessionsFromJson(brokenJsonPath));

        Directory.Delete(tempDir, recursive: true);
        Console.WriteLine();
    }

    // runs and prints assignment 8 demo (extensions, currying, linq reports, lazy yield, native c)
    public static void RunAssignment8(IReadOnlyList<NeuralSession> sessions)
    {
        Console.WriteLine("ASSIGNMENT 8: Extensions, Curried Ripperdocs, LINQ Reports, Lazy Yield & Native C");

        // 1. Currying & Partial Application (Viktor Vektor vs Fingers M.D.)
        var david = sessions[2]; // David Martinez
        double vikScore = RiskPolicies.ViktorVektorClinic(david);
        double fingersScore = RiskPolicies.FingersShadyClinic(david);
        Console.WriteLine("[1. Curried Ripperdoc Clinics on David Martinez (Sandevistan-X)]");
        Console.WriteLine($"  Viktor Vektor Clinic (Good Immunoblockers x0.7) : Risk = {vikScore:F1}");
        Console.WriteLine($"  Fingers M.D. Clinic  (Shady Parts x1.2)         : Risk = {fingersScore:F1}\n");

        // 2. Custom Extensions (KeepSuccesses + GetRipperdocAdvice)
        var diagnosedBatch = sessions.Select(s => RiskPipeline.DiagnoseOperator(sessions, s.OperatorId, RiskPolicies.SafeNet)).KeepSuccesses();
        Console.WriteLine($"[2. Custom Extensions] Filtered {diagnosedBatch.Count}/{sessions.Count} safe operator diagnoses via .KeepSuccesses()");
        Console.WriteLine($"  Lore Advice for {david.OperatorName}: {david.GetRipperdocAdvice()}\n");

        // 3. LINQ Report 1: Implant Threat Index (GroupBy, Sum, Average, OrderByDescending + ThenBy)
        Console.WriteLine("[3. LINQ Report 1: Cyberware Manufacturer Threat Ranking]");
        foreach (var item in LinqReports.BuildImplantThreatReport(sessions, RiskPolicies.Overclock))
        {
            Console.WriteLine($"  {item.ImplantModel,-37} | Tier: {item.Tier,-12} | Cases: {item.SessionCount} | Total Risk: {item.TotalRisk,5:F1} | Avg GHz: {item.AverageGhz:F2}");
        }

        // 4. LINQ Report 2: MaxTac High-Risk Watchlist (Where, Select, OrderByDescending + ThenBy)
        Console.WriteLine("\n[4. LINQ Report 2: MaxTac & Trauma Team High-Risk Watchlist (Risk >= 50)]");
        var watchlist = LinqReports.BuildMaxTacWatchlist(sessions, RiskPolicies.Overclock, 50.0);
        foreach (var w in watchlist)
        {
            Console.WriteLine($"  {w.OperatorId} | {w.OperatorName,-17} | Risk: {w.RiskScore,5:F1} | Action: {w.Advice}");
        }

        // 5. Lazy processing (yield return) + Materialization + File Export with using
        int stepsRun = 0;
        var lazyQuery = LinqReports.StreamEvaluatedSessions(sessions, RiskPolicies.SafeNet, () => stepsRun++);
        Console.WriteLine($"\n[5. Lazy Yield Stream] Query created. Steps executed before ToList(): {stepsRun}");
        var snapshot = lazyQuery.ToList();
        Console.WriteLine($"  Materialized snapshot with .ToList()! Steps executed now: {stepsRun} (Snapshot count: {snapshot.Count})");

        string reportPath = Path.Combine(AppContext.BaseDirectory, "maxtac_watchlist.json");
        if (LinqReports.ExportReportWithUsing(reportPath, watchlist) is Result<string, NeuralError>.Success saved)
            Console.WriteLine($"  Exported MaxTac Watchlist safely with 'using' StreamWriter -> {Path.GetFileName(saved.Value)}");

        // 6. Native C P/Invoke call (Humanity Stability Index)
        Console.WriteLine("\n[6. Native C Library (cybersignal.dll) P/Invoke Humanity Check]");
        PrintHumanityCheck(david, blockerDoses: 1);
        PrintHumanityCheck(sessions[1], blockerDoses: 2); // Lucy
    }

    private static void PrintPolicyTable(string policyName, IReadOnlyList<RiskAssessment> report)
    {
        Console.WriteLine($"Policy: {policyName}");
        foreach (var r in report)
        {
            Console.WriteLine($"  {r.SessionId} | {r.OperatorName,-17} | {r.ImplantModel,-37} | Risk: {r.RiskScore,5:F1} -> {RiskPolicies.ClassifyStatus(r.RiskScore)}");
            Console.WriteLine($"        Reason: {r.Reason}");
        }
        Console.WriteLine();
    }

    private static void PrintStepOutcome(string label, WorkflowOperationResult result) =>
        Console.WriteLine(result switch
        {
            WorkflowOperationResult.Success { Assessment: var a } =>
                $"           [{label}] ALLOWED: {a.SessionId} ({a.OperatorName}) -> Risk = {a.RiskScore:F1}",
            WorkflowOperationResult.Rejected { Reason: var r } =>
                $"           [{label}] REJECTED: {r}",
            _ => throw new InvalidOperationException("Unknown workflow result.")
        });

    private static void PrintDiagnosis(string label, Result<OperatorStatus, NeuralError> result) =>
        Console.WriteLine(result switch
        {
            Result<OperatorStatus, NeuralError>.Success { Value: var s } =>
                $"  [SUCCESS - {label}] {s.OperatorId} ({s.OperatorName}) | Risk: {s.RiskScore:F1} | {s.Status}",
            Result<OperatorStatus, NeuralError>.Failure { Error: var e } =>
                $"  [FAILURE - {label}] [{e.Code}] {e.Message}",
            _ => throw new InvalidOperationException("Unknown result.")
        });

    private static void PrintFileOutcome(string label, Result<IReadOnlyList<NeuralSession>, NeuralError> result) =>
        Console.WriteLine(result switch
        {
            Result<IReadOnlyList<NeuralSession>, NeuralError>.Success { Value: var list } =>
                $"  [FILE OK - {label}] Loaded {list.Count} sessions safely.",
            Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure { Error: var e } =>
                $"  [FILE ERROR - {label}] [{e.Code}] {e.Message}",
            _ => throw new InvalidOperationException("Unknown file result.")
        });

    private static void PrintHumanityCheck(NeuralSession s, int blockerDoses)
    {
        var res = CyberSignalNative.CheckHumanityStability(s, blockerDoses);
        Console.WriteLine(res switch
        {
            Result<int, NeuralError>.Success { Value: var h } =>
                $"  [Native C OK] {s.OperatorId} ({s.OperatorName}) with {blockerDoses}x Immunoblockers -> Humanity Index: {h}%",
            Result<int, NeuralError>.Failure { Error: var e } =>
                $"  [Native C Guard] [{e.Code}] {e.Message}",
            _ => throw new InvalidOperationException("Unknown native result.")
        });
    }
}