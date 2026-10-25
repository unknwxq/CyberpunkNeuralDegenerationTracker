
using SynapseSec.Models;
using SynapseSec.Policies;

namespace SynapseSec.Pipeline;

// creates sessions and checks if the data is correct
public static class SessionFactory
{
    public static SessionValidationResult Create(NeuralSession? input)
    {
        if (input is null)
            return new SessionValidationResult.Invalid(["Session cannot be null."]);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(input.OperatorId))
            errors.Add("Operator ID is required.");

        if (string.IsNullOrWhiteSpace(input.ImplantModel))
            errors.Add("Implant model is required.");

        if (!double.IsFinite(input.SessionDurationHours) || input.SessionDurationHours < 0)
            errors.Add("Session duration must be finite and non-negative.");

        if (!double.IsFinite(input.NeuralLoadGhz) || input.NeuralLoadGhz < 0)
            errors.Add("Neural load must be finite and non-negative.");

        if (!double.IsFinite(input.StressLevel) || input.StressLevel < 0 || input.StressLevel > 10)
            errors.Add("Stress level must be between 0 and 10.");

        if (errors.Count > 0)
            return new SessionValidationResult.Invalid(errors.AsReadOnly());

        string name = string.IsNullOrWhiteSpace(input.OperatorName) ? "Unknown" : input.OperatorName.Trim();

        return new SessionValidationResult.Valid(new ValidatedNeuralSession(
            input.OperatorId.Trim(),
            name,
            input.ImplantModel.Trim(),
            input.SessionDurationHours,
            input.NeuralLoadGhz,
            input.StressLevel));
    }

    // checks the session data and if the implant is certified
    public static Result<ValidatedNeuralSession, NeuralError> CreateResult(NeuralSession? input)
    {
        var basicValidation = Create(input);
        if (basicValidation is SessionValidationResult.Invalid invalid)
        {
            return new Result<ValidatedNeuralSession, NeuralError>.Failure(
                new NeuralError(ErrorCode.BrainwaveCorrupted, string.Join(" ", invalid.Errors)));
        }

        var session = ((SessionValidationResult.Valid)basicValidation).Session;

        // checks if the implant exists in the list
        if (!RiskPolicies.Implants.ContainsKey(session.ImplantModel))
        {
            return new Result<ValidatedNeuralSession, NeuralError>.Failure(
                new NeuralError(
                    ErrorCode.ImplantNotRecognized,
                    $"Implant '{session.ImplantModel}' for {session.OperatorId} is not in the certified registry."));
        }

        return new Result<ValidatedNeuralSession, NeuralError>.Success(session);
    }
}
