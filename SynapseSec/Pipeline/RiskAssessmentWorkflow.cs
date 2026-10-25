
using SynapseSec.Models;
using SynapseSec.Policies;

namespace SynapseSec.Pipeline;

// shows which step the workflow is on
public enum WorkflowState
{
    NotReady,
    Ready,
    Completed
}

// manages the session and risk assessment process
public sealed class RiskAssessmentWorkflow
{
    private ValidatedNeuralSession? _session;

    public WorkflowState State { get; private set; } = WorkflowState.NotReady;
    public RiskAssessment? Assessment { get; private set; }

    public SessionValidationResult Prepare(NeuralSession? input)
    {
        // session can only be prepared at the start
        if (State != WorkflowState.NotReady)
            return new SessionValidationResult.Invalid([$"Cannot prepare a session in state {State}."]);

        var result = SessionFactory.Create(input);
        if (result is SessionValidationResult.Valid valid)
        {
            _session = valid.Session;
            State = WorkflowState.Ready;
        }

        return result;
    }

    public WorkflowOperationResult Assess(Func<ValidatedNeuralSession, double> policy)
    {
        // checks if the session is ready for risk calculation
        if (State != WorkflowState.Ready || _session is null)
            return new WorkflowOperationResult.Rejected($"Assessment is not allowed in state {State}.");

        if (policy is null)
            return new WorkflowOperationResult.Rejected("Risk policy cannot be null.");

        try
        {
            // calculates the risk score using the given policy
            double score = policy(_session);
            if (!double.IsFinite(score))
                return new WorkflowOperationResult.Rejected("Risk policy returned a non-finite score.");

            var raw = _session.ToRaw();

            // saves the result with operator info and risk reason
            Assessment = new RiskAssessment(
                _session.OperatorId,
                score,
                _session.OperatorName,
                _session.ImplantModel,
                RiskPolicies.ExplainReason(raw, score));

            State = WorkflowState.Completed;
            return new WorkflowOperationResult.Success(Assessment);
        }
        catch (Exception ex)
        {
            return new WorkflowOperationResult.Rejected($"Risk calculation failed: {ex.Message}");
        }
    }
}
