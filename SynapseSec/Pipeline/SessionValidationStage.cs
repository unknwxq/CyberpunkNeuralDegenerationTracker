
using SynapseSec.Models;

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
            var result = SessionFactory.Create(input);

            switch (result)
            {
                case SessionValidationResult.Valid valid:
                    normalized.Add(new NeuralSession
                    {
                        OperatorId = valid.Session.OperatorId,
                        ImplantModel = valid.Session.ImplantModel,
                        SessionDurationHours =
                            valid.Session.SessionDurationHours,
                        NeuralLoadGhz = valid.Session.NeuralLoadGhz,
                        StressLevel = valid.Session.StressLevel
                    });
                    break;

                case SessionValidationResult.Invalid invalid:
                    throw new ArgumentException(
                        string.Join(" ", invalid.Errors),
                        nameof(sessions));

                default:
                    throw new InvalidOperationException(
                        "Unknown validation result.");
            }
        }

        return Array.AsReadOnly(normalized.ToArray());
    }
}
