
namespace SynapseSec.Models;

//closed set of possible validation results
public abstract record SessionValidationResult
{
    private SessionValidationResult()
    {
    }

    public sealed record Valid(
        ValidatedNeuralSession Session) : SessionValidationResult;

    public sealed record Invalid(
        IReadOnlyList<string> Errors) : SessionValidationResult;
}
