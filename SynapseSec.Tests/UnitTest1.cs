
using System.Text.Json;
using SynapseSec.Models;
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

    // assignment 7 tests

    [Fact]
    public void CompleteSuccess_IntegratedResultChain_ProducesOperatorStatus()
    {
        var sessions = SampleData.CreateSessions();
        var result = RiskPipeline.DiagnoseOperator(sessions, "NC-02", RiskPolicies.SafeNet);

        var success = Assert.IsType<Result<OperatorStatus, NeuralError>.Success>(result);
        Assert.Equal("Lucy Kushinada", success.Value.OperatorName);
        Assert.Equal(40.0, success.Value.RiskScore);
        Assert.Contains("NORMAL", success.Value.Status);
    }

    [Fact]
    public void DomainFailures_AllThreeDistinctFailures_ReturnExpectedErrorCodes()
    {
        // corrupted brainwave
        var corrupted = new[] { CreateSession(id: "NC-10", duration: -5) };
        var r1 = RiskPipeline.DiagnoseOperator(corrupted, "NC-10", RiskPolicies.SafeNet);
        var f1 = Assert.IsType<Result<OperatorStatus, NeuralError>.Failure>(r1);
        Assert.Equal(ErrorCode.BrainwaveCorrupted, f1.Error.Code);

        // unknown implant
        var unknownChip = new[] { CreateSession(id: "NC-11", implant: "Bootleg-Cyberdeck-v0") };
        var r2 = RiskPipeline.DiagnoseOperator(unknownChip, "NC-11", RiskPolicies.SafeNet);
        var f2 = Assert.IsType<Result<OperatorStatus, NeuralError>.Failure>(r2);
        Assert.Equal(ErrorCode.ImplantNotRecognized, f2.Error.Code);

        // mental breakdown
        var sessions = SampleData.CreateSessions();
        var r3 = RiskPipeline.DiagnoseOperator(sessions, "NC-03", RiskPolicies.Overclock);
        var f3 = Assert.IsType<Result<OperatorStatus, NeuralError>.Failure>(r3);
        Assert.Equal(ErrorCode.MentalBreakdown, f3.Error.Code);
    }

    [Fact]
    public void Lookup_PresentZeroValuesAndAbsentOperator_AreHandledExplicitly()
    {
        var sessions = SampleData.CreateSessions();

        // zero values should still count as a valid result
        var presentZero = RiskPipeline.DiagnoseOperator(sessions, "NC-06", RiskPolicies.SafeNet);
        var ok = Assert.IsType<Result<OperatorStatus, NeuralError>.Success>(presentZero);
        Assert.Equal(0, ok.Value.RiskScore);
        Assert.Equal("Kiwi", ok.Value.OperatorName);

        // operator does not exist
        var absent = RiskPipeline.DiagnoseOperator(sessions, "NC-999", RiskPolicies.SafeNet);
        var fail = Assert.IsType<Result<OperatorStatus, NeuralError>.Failure>(absent);
        Assert.Equal(ErrorCode.OperatorNotFound, fail.Error.Code);
    }

    [Fact]
    public void MapAndBind_OnSuccess_ExecuteSuppliedFunctions()
    {
        var start = new Result<int, NeuralError>.Success(10);

        var bound = start.Bind(x => new Result<int, NeuralError>.Success(x * 2));
        var mapped = bound.Map(x => $"Score: {x}");

        var success = Assert.IsType<Result<string, NeuralError>.Success>(mapped);
        Assert.Equal("Score: 20", success.Value);
    }

    [Fact]
    public void FailurePropagation_SkipsSubsequentMapAndBindFunctions()
    {
        var error = new NeuralError(ErrorCode.BrainwaveCorrupted, "Signal lost");
        Result<int, NeuralError> failed = new Result<int, NeuralError>.Failure(error);

        bool bindCalled = false;
        bool mapCalled = false;

        var afterBind = failed.Bind(x =>
        {
            bindCalled = true;
            return new Result<int, NeuralError>.Success(x + 10);
        });

        var afterMap = afterBind.Map(x =>
        {
            mapCalled = true;
            return x * 2;
        });

        Assert.False(bindCalled);
        Assert.False(mapCalled);
        var finalFailure = Assert.IsType<Result<int, NeuralError>.Failure>(afterMap);
        Assert.Equal(ErrorCode.BrainwaveCorrupted, finalFailure.Error.Code);
    }

    [Fact]
    public void FileBoundary_MissingFile_ReturnsFileNotFoundFailure()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid()}.json");

        var result = RiskPipeline.LoadSessionsFromJson(missingPath);

        var failure = Assert.IsType<Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure>(result);
        Assert.Equal(ErrorCode.FileNotFound, failure.Error.Code);
    }

    [Fact]
    public void FileBoundary_MalformedJson_ReturnsMalformedFileFailure()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"malformed_{Guid.NewGuid()}.json");
        try
        {
            File.WriteAllText(tempFile, "{ broken_json: [ 123 ");

            var result = RiskPipeline.LoadSessionsFromJson(tempFile);

            var failure = Assert.IsType<Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure>(result);
            Assert.Equal(ErrorCode.MalformedFile, failure.Error.Code);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void FileBoundary_ValidJson_LoadsSessionsCorrectly()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"valid_{Guid.NewGuid()}.json");
        try
        {
            var original = SampleData.CreateSessions();
            File.WriteAllText(tempFile, JsonSerializer.Serialize(original));

            var result = RiskPipeline.LoadSessionsFromJson(tempFile);

            var success = Assert.IsType<Result<IReadOnlyList<NeuralSession>, NeuralError>.Success>(result);
            Assert.Equal(original.Count, success.Value.Count);
            Assert.Equal("David Martinez", success.Value[2].OperatorName);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
