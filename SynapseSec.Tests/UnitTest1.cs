
using SynapseSec.Models;
using SynapseSec.Pipeline;
using SynapseSec.Policies;

namespace SynapseSec.Tests;

public class RiskPipelineTests
{
    private static NeuralSession CreateSession(
        string id = "OP-01",
        double stress = 6,
        double duration = 2,
        double load = 5)
    {
        return new NeuralSession
        {
            OperatorId = id,
            ImplantModel = "Neuro-X",
            StressLevel = stress,
            SessionDurationHours = duration,
            NeuralLoadGhz = load
        };
    }

    [Fact]
    public void NormalWorkflow_ProducesExpectedReport()
    {
        var report = RiskPipeline.Run(
            new[] { CreateSession() },
            RiskPolicies.SafeNet);

        Assert.Single(report);
        Assert.Equal("OP-01", report[0].SessionId);
        Assert.Equal(64, report[0].RiskScore);
    }

    [Fact]
    public void EmptyDataset_ReturnsEmptyReport()
    {
        var report = RiskPipeline.Run(
            Array.Empty<NeuralSession>(),
            RiskPolicies.SafeNet);

        Assert.Empty(report);
    }

    [Fact]
    public void BoundaryValues_AreAccepted()
    {
        var report = RiskPipeline.Run(
            new[] { CreateSession(stress: 0, duration: 0, load: 0) },
            RiskPolicies.SafeNet);

        Assert.Single(report);
        Assert.Equal(0, report[0].RiskScore);
    }

    [Fact]
    public void InvalidAndMissingInput_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RiskPipeline.Run(null!, RiskPolicies.SafeNet));

        Assert.Throws<ArgumentNullException>(() =>
            RiskPipeline.Run(
                Array.Empty<NeuralSession>(), null!));

        Assert.Throws<ArgumentException>(() =>
            RiskPipeline.Run(
                new[] { CreateSession(stress: 11) },
                RiskPolicies.SafeNet));

        Assert.Throws<ArgumentException>(() =>
            RiskPipeline.Run(
                new[] { CreateSession(id: " ") },
                RiskPolicies.SafeNet));
    }

    [Fact]
    public void TwoPolicies_ProduceDifferentResults()
    {
        var sessions = new[] { CreateSession() };

        var safe = RiskPipeline.Run(
            sessions, RiskPolicies.SafeNet);

        var overclock = RiskPipeline.Run(
            sessions, RiskPolicies.Overclock);

        Assert.Equal(64, safe[0].RiskScore);
        Assert.Equal(70, overclock[0].RiskScore);
        Assert.NotEqual(
            safe[0].RiskScore, overclock[0].RiskScore);
    }

    [Fact]
    public void Validation_DoesNotModifyCallerOwnedData()
    {
        var original = CreateSession(id: " OP-01 ");
        var sessions = new[] { original };

        var result =
            SessionValidationStage.ValidateAndNormalize(sessions);

        Assert.Equal(" OP-01 ", original.OperatorId);
        Assert.Equal("OP-01", result[0].OperatorId);
        Assert.NotSame(original, result[0]);
        Assert.Same(original, sessions[0]);
    }

    // assigment 6 test

    
    [Fact]
    public void Factory_ValidSession_ReturnsValidatedSession()
    {
        var result = SessionFactory.Create(CreateSession());

        var valid = Assert.IsType<SessionValidationResult.Valid>(result);

        Assert.Equal("OP-01", valid.Session.OperatorId);
        Assert.Equal("Neuro-X", valid.Session.ImplantModel);
    }

    [Fact]
    public void Factory_MissingOperatorId_ReturnsInvalid()
    {
        var result = SessionFactory.Create(CreateSession(id: " "));

        var invalid = Assert.IsType<SessionValidationResult.Invalid>(result);

        Assert.Contains(invalid.Errors, error =>
            error.Contains("Operator ID"));
    }

    [Fact]
    public void Factory_InvalidStress_ReturnsInvalid()
    {
        var result = SessionFactory.Create(CreateSession(stress: 11));

        Assert.IsType<SessionValidationResult.Invalid>(result);
    }

    [Fact]
    public void Factory_NegativeDuration_ReturnsInvalid()
    {
        var result = SessionFactory.Create(
            CreateSession(duration: -1));

        Assert.IsType<SessionValidationResult.Invalid>(result);
    }

    [Fact]
    public void Factory_NonFiniteLoad_ReturnsInvalid()
    {
        var result = SessionFactory.Create(
            CreateSession(load: double.NaN));

        Assert.IsType<SessionValidationResult.Invalid>(result);
    }

    [Fact]
    public void Workflow_AssessmentBeforePreparation_IsRejected()
    {
        var workflow = new RiskAssessmentWorkflow();

        var result = workflow.Assess(session => 10);

        Assert.IsType<WorkflowOperationResult.Rejected>(result);
        Assert.Equal(WorkflowState.NotReady, workflow.State);
        Assert.Null(workflow.Assessment);
    }

    [Fact]
    public void Workflow_ValidSession_CompletesAssessment()
    {
        var workflow = new RiskAssessmentWorkflow();

        var preparation = workflow.Prepare(CreateSession());

        Assert.IsType<SessionValidationResult.Valid>(preparation);
        Assert.Equal(WorkflowState.Ready, workflow.State);

        var result = workflow.Assess(
            session => 10 * session.StressLevel
                + 2 * session.SessionDurationHours);

        var success = Assert.IsType<WorkflowOperationResult.Success>(result);

        Assert.Equal(64, success.Assessment.RiskScore);
        Assert.Equal(WorkflowState.Completed, workflow.State);
        Assert.Equal(success.Assessment, workflow.Assessment);
    }

    [Fact]
    public void Workflow_RepeatedAssessment_IsRejected()
    {
        var workflow = new RiskAssessmentWorkflow();
        workflow.Prepare(CreateSession());

        var first = workflow.Assess(session => 42);
        var original = Assert.IsType<WorkflowOperationResult.Success>(first);

        var second = workflow.Assess(session => 100);

        Assert.IsType<WorkflowOperationResult.Rejected>(second);
        Assert.Equal(WorkflowState.Completed, workflow.State);
        Assert.Equal(original.Assessment, workflow.Assessment);
        Assert.Equal(42, workflow.Assessment!.RiskScore);
    }

    [Fact]
    public void Workflow_FailedAssessment_PreservesReadyState()
    {
        var workflow = new RiskAssessmentWorkflow();
        workflow.Prepare(CreateSession());

        var result = workflow.Assess(session => double.NaN);

        Assert.IsType<WorkflowOperationResult.Rejected>(result);
        Assert.Equal(WorkflowState.Ready, workflow.State);
        Assert.Null(workflow.Assessment);
    }

    [Fact]
    public void Workflow_InvalidSession_DoesNotBecomeReady()
    {
        var workflow = new RiskAssessmentWorkflow();

        var result = workflow.Prepare(CreateSession(stress: 15));

        Assert.IsType<SessionValidationResult.Invalid>(result);
        Assert.Equal(WorkflowState.NotReady, workflow.State);
        Assert.Null(workflow.Assessment);
    }

    [Fact]
    public void Factory_BoundaryValues_AreAccepted()
    {
        var result = SessionFactory.Create(
            CreateSession(stress: 0, duration: 0, load: 0));

        Assert.IsType<SessionValidationResult.Valid>(result);
    }

}
