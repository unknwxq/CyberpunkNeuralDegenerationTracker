
using SynapseSec.Models;
using SynapseSec.Policies;

namespace SynapseSec.Pipeline;

//coordinates validation, assessment, and report generation
public static class RiskPipeline
{
    public static IReadOnlyList<RiskAssessment> Run(
        IReadOnlyList<NeuralSession> sessions,
        Func<NeuralSession, double> policy)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(policy);

        var normalizedSessions = new List<NeuralSession>();
        var scores = new List<double>();

        foreach (var input in sessions)
        {
            var workflow = new RiskAssessmentWorkflow();

            var preparation = workflow.Prepare(input);

            switch (preparation)
            {
                case SessionValidationResult.Valid valid:
                    Func<ValidatedNeuralSession, double> safePolicy =
                        session => policy(new NeuralSession
                        {
                            OperatorId = session.OperatorId,
                            ImplantModel = session.ImplantModel,
                            SessionDurationHours =
                                session.SessionDurationHours,
                            NeuralLoadGhz = session.NeuralLoadGhz,
                            StressLevel = session.StressLevel
                        });

                    var operation = workflow.Assess(
                        session => RiskCalculator.Calculate(
                            session, safePolicy));

                    switch (operation)
                    {
                        case WorkflowOperationResult.Success success:
                            normalizedSessions.Add(new NeuralSession
                            {
                                OperatorId = valid.Session.OperatorId,
                                ImplantModel = valid.Session.ImplantModel,
                                SessionDurationHours =
                                    valid.Session.SessionDurationHours,
                                NeuralLoadGhz =
                                    valid.Session.NeuralLoadGhz,
                                StressLevel = valid.Session.StressLevel
                            });

                            scores.Add(success.Assessment.RiskScore);
                            break;

                        case WorkflowOperationResult.Rejected rejected:
                            throw new InvalidOperationException(
                                rejected.Reason);

                        default:
                            throw new InvalidOperationException(
                                "Unknown workflow operation result.");
                    }

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

        return RiskReportStage.CreateReport(
            normalizedSessions,
            scores);
    }
}
