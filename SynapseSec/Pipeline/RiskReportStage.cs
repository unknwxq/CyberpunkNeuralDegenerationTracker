
using SynapseSec.Models;

namespace SynapseSec.Pipeline;

//converts calculated scores to ordered report
public static class RiskReportStage
{
    public static IReadOnlyList<RiskAssessment> CreateReport(
        IReadOnlyList<NeuralSession> sessions,
        IReadOnlyList<double> scores)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(scores);

        if (sessions.Count != scores.Count)
        {
            throw new ArgumentException(
                "The number of sessions must match the number of scores.");
        }

        return sessions
            .Select((session, index) =>
                new RiskAssessment(session.OperatorId, scores[index]))
            .OrderByDescending(result => result.RiskScore)
            .ToArray();
    }
}
