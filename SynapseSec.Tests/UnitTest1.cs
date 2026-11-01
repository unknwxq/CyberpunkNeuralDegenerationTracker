using System.Text.Json;
using SynapseSec.Models;
using SynapseSec.Native;
using SynapseSec.Pipeline;
using SynapseSec.Policies;

namespace SynapseSec.Tests;

public class RiskPipelineTests
{
    private static NeuralSession CreateSession(
        string id = "NC-01",
        string name = "V (Valerie)",
        string implant = "Arasaka Mk.IV (Corporate)",
        double stress = 6,
        double duration = 2,
        double load = 5) => new()
    {
        OperatorId = id,
        OperatorName = name,
        ImplantModel = implant,
        StressLevel = stress,
        SessionDurationHours = duration,
        NeuralLoadGhz = load
    };

    // --- assignment 5 & 6 core tests ---

    [Fact]
    public void NormalWorkflow_And_TwoPolicies_ProduceExpectedResults()
    {
        var sessions = new[] { CreateSession() };
        var safe = RiskPipeline.Run(sessions, RiskPolicies.SafeNet);
        var overclock = RiskPipeline.Run(sessions, RiskPolicies.Overclock);

        Assert.Single(safe);
        Assert.Equal(64, safe[0].RiskScore);
        Assert.Equal(70, overclock[0].RiskScore);
    }

    [Fact]
    public void Workflow_EnforcesSafeOrder_And_RejectsInvalidStates()
    {
        var workflow = new RiskAssessmentWorkflow();
        Assert.IsType<WorkflowOperationResult.Rejected>(workflow.Assess(_ => 10));

        Assert.IsType<SessionValidationResult.Valid>(workflow.Prepare(CreateSession()));
        Assert.IsType<WorkflowOperationResult.Success>(workflow.Assess(_ => 64));
        Assert.IsType<WorkflowOperationResult.Rejected>(workflow.Assess(_ => 100));
    }

    // --- assignment 7 tests ---

    [Fact]
    public void CompleteSuccess_IntegratedResultChain_ProducesOperatorStatus()
    {
        var sessions = SampleData.CreateSessions();
        var result = RiskPipeline.DiagnoseOperator(sessions, "NC-02", RiskPolicies.SafeNet);

        var success = Assert.IsType<Result<OperatorStatus, NeuralError>.Success>(result);
        Assert.Equal("Lucy Kushinada", success.Value.OperatorName);
        Assert.Equal(40.0, success.Value.RiskScore);
    }

    [Fact]
    public void DomainFailures_AllThreeDistinctFailures_ReturnExpectedErrorCodes()
    {
        var corrupted = new[] { CreateSession(id: "NC-10", duration: -5) };
        var f1 = Assert.IsType<Result<OperatorStatus, NeuralError>.Failure>(
            RiskPipeline.DiagnoseOperator(corrupted, "NC-10", RiskPolicies.SafeNet));
        Assert.Equal(ErrorCode.BrainwaveCorrupted, f1.Error.Code);

        var unknownChip = new[] { CreateSession(id: "NC-11", implant: "Bootleg-Cyberdeck-v0") };
        var f2 = Assert.IsType<Result<OperatorStatus, NeuralError>.Failure>(
            RiskPipeline.DiagnoseOperator(unknownChip, "NC-11", RiskPolicies.SafeNet));
        Assert.Equal(ErrorCode.ImplantNotRecognized, f2.Error.Code);

        var sessions = SampleData.CreateSessions();
        var f3 = Assert.IsType<Result<OperatorStatus, NeuralError>.Failure>(
            RiskPipeline.DiagnoseOperator(sessions, "NC-03", RiskPolicies.Overclock));
        Assert.Equal(ErrorCode.MentalBreakdown, f3.Error.Code);
    }

    [Fact]
    public void Lookup_PresentZeroValuesAndAbsentOperator_AreHandledExplicitly()
    {
        var sessions = SampleData.CreateSessions();

        var ok = Assert.IsType<Result<OperatorStatus, NeuralError>.Success>(
            RiskPipeline.DiagnoseOperator(sessions, "NC-06", RiskPolicies.SafeNet));
        Assert.Equal(0, ok.Value.RiskScore);

        var fail = Assert.IsType<Result<OperatorStatus, NeuralError>.Failure>(
            RiskPipeline.DiagnoseOperator(sessions, "NC-999", RiskPolicies.SafeNet));
        Assert.Equal(ErrorCode.OperatorNotFound, fail.Error.Code);
    }

    [Fact]
    public void FailurePropagation_SkipsSubsequentMapAndBindFunctions()
    {
        Result<int, NeuralError> failed = new Result<int, NeuralError>.Failure(
            new NeuralError(ErrorCode.BrainwaveCorrupted, "Signal lost"));

        bool called = false;
        var after = failed
            .Bind(x => { called = true; return new Result<int, NeuralError>.Success(x + 1); })
            .Map(x => { called = true; return x * 2; });

        Assert.False(called);
        Assert.IsType<Result<int, NeuralError>.Failure>(after);
    }

    [Fact]
    public void FileBoundary_MissingAndMalformedFiles_ReturnSafeFailures()
    {
        string missing = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid()}.json");
        var f1 = Assert.IsType<Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure>(
            RiskPipeline.LoadSessionsFromJson(missing));
        Assert.Equal(ErrorCode.FileNotFound, f1.Error.Code);

        string tempFile = Path.Combine(Path.GetTempPath(), $"malformed_{Guid.NewGuid()}.json");
        try
        {
            File.WriteAllText(tempFile, "{ broken_json: [ 123 ");
            var f2 = Assert.IsType<Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure>(
                LinqReports.LoadSnapshotWithUsing(tempFile));
            Assert.Equal(ErrorCode.MalformedFile, f2.Error.Code);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    // --- assignment 8 tests (extensions, linq reports, currying, yield, using, native c) ---

    [Fact]
    public void CustomExtensions_GetRipperdocAdvice_And_KeepSuccesses_WorkCorrectly()
    {
        var david = SampleData.CreateSessions()[2];
        Assert.Contains("MAXTAC", david.GetRipperdocAdvice());

        Result<int, string>[] results =
        [
            new Result<int, string>.Success(10),
            new Result<int, string>.Failure("err"),
            new Result<int, string>.Success(20)
        ];

        var successes = results.KeepSuccesses();
        Assert.Equal([10, 20], successes);
    }

    [Fact]
    public void CurriedRipperdocScorer_PartialApplications_ProduceDifferentScoresOnSameInput()
    {
        var session = CreateSession(stress: 6, load: 5.5);

        double vikScore = RiskPolicies.ViktorVektorClinic(session);
        double fingersScore = RiskPolicies.FingersShadyClinic(session);

        Assert.NotEqual(vikScore, fingersScore);
        Assert.True(fingersScore > vikScore);
    }

    [Fact]
    public void LinqReports_HandleNormal_Empty_NoMatches_And_TiedOrdering()
    {
        var sessions = SampleData.CreateSessions();

        var report1 = LinqReports.BuildImplantThreatReport(sessions, RiskPolicies.Overclock);
        Assert.Equal(5, report1.Count);
        Assert.Equal("Militech Sandevistan-X (NextGen-Beta)", report1[0].ImplantModel);
        Assert.Equal(315.0, report1[0].TotalRisk);

        // empty dataset check
        Assert.Empty(LinqReports.BuildImplantThreatReport([], RiskPolicies.SafeNet));

        // no matches check (threshold 999)
        Assert.Empty(LinqReports.BuildMaxTacWatchlist(sessions, RiskPolicies.SafeNet, minRiskThreshold: 999));

        // deterministic tie-breaker check when scores are equal
        var tied = new[]
        {
            CreateSession(id: "NC-B", stress: 6, duration: 2),
            CreateSession(id: "NC-A", stress: 6, duration: 2)
        };
        var watchlist = LinqReports.BuildMaxTacWatchlist(tied, RiskPolicies.SafeNet, 50);
        Assert.Equal("NC-A", watchlist[0].OperatorId);
        Assert.Equal("NC-B", watchlist[1].OperatorId);
    }

    [Fact]
    public void DeferredExecution_YieldStream_DoesNotRunUntilEnumerated()
    {
        var sessions = SampleData.CreateSessions();
        int counter = 0;

        var lazyStream = LinqReports.StreamEvaluatedSessions(sessions, RiskPolicies.SafeNet, () => counter++);

        // before enumeration counter must be 0
        Assert.Equal(0, counter);

        // materialising into a snapshot runs the generator once
        var snapshot = lazyStream.ToList();
        Assert.Equal(sessions.Count, counter);
        Assert.Equal(sessions.Count, snapshot.Count);

        // reading snapshot again does not re-trigger generator
        _ = snapshot.Count();
        Assert.Equal(sessions.Count, counter);
    }

    [Fact]
    public void ResourceLifetime_ExportAndLoadWithUsing_ReleasesFileHandleImmediately()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"export_{Guid.NewGuid()}.json");
        try
        {
            var sessions = SampleData.CreateSessions();
            var exportRes = LinqReports.ExportReportWithUsing(tempFile, sessions);
            Assert.IsType<Result<string, NeuralError>.Success>(exportRes);

            // if 'using' properly closed the file handle, we can immediately read and delete it without IOException
            var loadRes = LinqReports.LoadSnapshotWithUsing(tempFile);
            var ok = Assert.IsType<Result<IReadOnlyList<NeuralSession>, NeuralError>.Success>(loadRes);
            Assert.Equal(sessions.Count, ok.Value.Count);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void NativeIntegration_CalculatesExpectedHumanity_And_HandlesFailuresSafely()
    {
        var session = CreateSession(stress: 5, load: 4);

        // 100 - (5*6) - (4*5) + (2*6) = 100 - 30 - 20 + 12 = 62
        var ok = CyberSignalNative.CheckHumanityStability(
            session,
            blockerDoses: 2,
            nativeCaller: (stress, ghz, doses) => 100 - (stress * 6) - (ghz * 5) + (doses * 6));

        var success = Assert.IsType<Result<int, NeuralError>.Success>(ok);
        Assert.Equal(62, success.Value);

        // invalid negative blocker doses rejected by adapter
        var fail = CyberSignalNative.CheckHumanityStability(session, blockerDoses: -1);
        var err = Assert.IsType<Result<int, NeuralError>.Failure>(fail);
        Assert.Equal(ErrorCode.NativeCallFailed, err.Error.Code);
    }
}