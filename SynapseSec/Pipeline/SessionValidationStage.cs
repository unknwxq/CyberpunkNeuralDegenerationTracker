using SynapseSec.Models;
using SynapseSec.Policies;

namespace SynapseSec.Pipeline;

public static class SessionValidationStage
{
    public static IReadOnlyList<NeuralSession> ValidateAndNormalize(
        IReadOnlyList<NeuralSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var normalized = new List<NeuralSession>();

        foreach (var input in sessions)
        {
            var validSession = SessionFactory.Create(input) switch
            {
                SessionValidationResult.Valid valid => valid.Session.ToRaw(),
                SessionValidationResult.Invalid invalid =>
                    throw new ArgumentException(string.Join(" ", invalid.Errors), nameof(sessions)),
                _ => throw new InvalidOperationException("Unknown validation result.")
            };

            normalized.Add(validSession);
        }

        return normalized.AsReadOnly();
    }
}