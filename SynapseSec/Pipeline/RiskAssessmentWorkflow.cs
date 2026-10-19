
using SynapseSec.Models;

namespace SynapseSec.Pipeline;

public enum WorkflowState
{
    NotReady,
    Ready,
    Completed
}

//controls the order of operations for one session
public sealed class RiskAssessmentWorkflow
{
    private ValidatedNeuralSession? _session;
    private RiskAssessment? _assessment;

    public WorkflowState State { get; private set; } =
        WorkflowState.NotReady;

    public RiskAssessment? Assessment => _assessment;

    public SessionValidationResult Prepare(NeuralSession? input)
    {
        if (State != WorkflowState.NotReady)
        {
            return new SessionValidationResult.Invalid(
                new[]
                {
                    $"Cannot prepare a session in state {State}."
                });
        }

        var result = SessionFactory.Create(input);

        switch (result)
        {
            case SessionValidationResult.Valid valid:
                _session = valid.Session;
                State = WorkflowState.Ready;
                return valid;

            case SessionValidationResult.Invalid invalid:
                return invalid;

            default:
                throw new InvalidOperationException(
                    "Unknown validation result.");
        }
    }

    public WorkflowOperationResult Assess(
        Func<ValidatedNeuralSession, double> policy)
    {
        if (State != WorkflowState.Ready || _session is null)
        {
            return new WorkflowOperationResult.Rejected(
                $"Assessment is not allowed in state {State}.");
        }

        if (policy is null)
        {
            return new WorkflowOperationResult.Rejected(
                "Risk policy cannot be null.");
        }

        try
        {
            double score = policy(_session);

            if (!double.IsFinite(score))
            {
                return new WorkflowOperationResult.Rejected(
                    "Risk policy returned a non-finite score.");
            }

            var assessment = new RiskAssessment(
                _session.OperatorId,
                score);


            _assessment = assessment;
            State = WorkflowState.Completed;

            return new WorkflowOperationResult.Success(assessment);
        }
        catch (Exception exception)
        {
            return new WorkflowOperationResult.Rejected(
                $"Risk calculation failed: {exception.Message}");
        }
    }
}
