
using SynapseSec.Models;
using SynapseSec.Policies;

namespace SynapseSec.Pipeline;

// creates a report with risk scores for all sessions
public static class RiskReportStage
{
    public static IReadOnlyList<RiskAssessment> CreateReport(
        IReadOnlyList<NeuralSession> sessions,
        IReadOnlyList<double> scores)
    {
        // checks if the input lists are valid
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(scores);

        if (sessions.Count != scores.Count)
            throw new ArgumentException("The number of sessions must match the number of scores.");

        // creates the report and sorts it from highest risk to lowest
        return sessions 
            .Select((s, i) => new RiskAssessment(
                s.OperatorId,
                scores[i],
                s.OperatorName,
                s.ImplantModel,
                RiskPolicies.ExplainReason(s, scores[i])))
            .OrderByDescending(r => r.RiskScore)
            .ToArray();
    }
}
