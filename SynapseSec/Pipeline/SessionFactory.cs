
using SynapseSec.Models;

namespace SynapseSec.Pipeline;

// validates input before creating a trusted session
public static class SessionFactory
{
    public static SessionValidationResult Create(NeuralSession? input)
    {
        if (input is null)
        {
            return Invalid("Session cannot be null.");
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(input.OperatorId))
        {
            errors.Add("Operator ID is required.");
        }

        if (string.IsNullOrWhiteSpace(input.ImplantModel))
        {
            errors.Add("Implant model is required.");
        }

        if (!double.IsFinite(input.SessionDurationHours) ||
            input.SessionDurationHours < 0)
        {
            errors.Add(
                "Session duration must be finite and non-negative.");
        }

        if (!double.IsFinite(input.NeuralLoadGhz) ||
            input.NeuralLoadGhz < 0)
        {
            errors.Add(
                "Neural load must be finite and non-negative.");
        }

        if (!double.IsFinite(input.StressLevel) ||
            input.StressLevel < 0 ||
            input.StressLevel > 10)
        {
            errors.Add("Stress level must be between 0 and 10.");
        }

        if (errors.Count > 0)
        {
            return new SessionValidationResult.Invalid(
                Array.AsReadOnly(errors.ToArray()));
        }

        var validatedSession = new ValidatedNeuralSession(
            input.OperatorId.Trim(),
            input.ImplantModel.Trim(),
            input.SessionDurationHours,
            input.NeuralLoadGhz,
            input.StressLevel);

        return new SessionValidationResult.Valid(validatedSession);
    }

    private static SessionValidationResult.Invalid Invalid(string error)
    {
        return new SessionValidationResult.Invalid(
            Array.AsReadOnly(new[] { error }));
    }
}
