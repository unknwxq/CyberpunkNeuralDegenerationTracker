
namespace SynapseSec.Models;

//result of an operation in the workflow
public abstract record WorkflowOperationResult
{
    private WorkflowOperationResult()
    {
    }

    public sealed record Success(
        RiskAssessment Assessment) : WorkflowOperationResult;

    public sealed record Rejected(
        string Reason) : WorkflowOperationResult;
}
