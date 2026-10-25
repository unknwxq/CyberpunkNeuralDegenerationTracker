
using System.Text.Json;
using SynapseSec.Models;
using SynapseSec.Policies;

namespace SynapseSec.Pipeline;

// runs the full risk calculation process
public static class RiskPipeline
{
    // processes all sessions and creates a sorted report
    public static IReadOnlyList<RiskAssessment> Run(
        IReadOnlyList<NeuralSession> sessions,
        Func<NeuralSession, double> policy)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(policy);

        var safePolicy = policy.Compose<ValidatedNeuralSession, NeuralSession, double>(s => s.ToRaw());
        var normalizedSessions = new List<NeuralSession>();
        var scores = new List<double>();

        // checks each session and calculates its risk score
        foreach (var input in sessions)
        {
            var workflow = new RiskAssessmentWorkflow();

            var validSession = workflow.Prepare(input) switch
            {
                SessionValidationResult.Valid v => v.Session,
                SessionValidationResult.Invalid inv =>
                    throw new ArgumentException(string.Join(" ", inv.Errors), nameof(sessions)),
                _ => throw new InvalidOperationException("Unknown validation result.")
            };

            var assessment = workflow.Assess(s => RiskCalculator.Calculate(s, safePolicy)) switch
            {
                WorkflowOperationResult.Success s => s.Assessment,
                WorkflowOperationResult.Rejected r => throw new InvalidOperationException(r.Reason),
                _ => throw new InvalidOperationException("Unknown workflow operation result.")
            };

            normalizedSessions.Add(validSession.ToRaw());
            scores.Add(assessment.RiskScore);
        }

        return RiskReportStage.CreateReport(normalizedSessions, scores);
    }

    // searches for a session by operator id
    public static LookupResult<NeuralSession> LookupSession(
        IReadOnlyList<NeuralSession> sessions,
        string operatorId)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        if (string.IsNullOrWhiteSpace(operatorId))
            return new LookupResult<NeuralSession>.NotFound(operatorId ?? "");

        var match = sessions.FirstOrDefault(s =>
            string.Equals(s.OperatorId?.Trim(), operatorId.Trim(), StringComparison.OrdinalIgnoreCase));

        return match is not null
            ? new LookupResult<NeuralSession>.Found(match)
            : new LookupResult<NeuralSession>.NotFound(operatorId.Trim());
    }

    // returns the session or an error if the operator was not found
    public static Result<NeuralSession, NeuralError> RequireSession(
        IReadOnlyList<NeuralSession> sessions,
        string operatorId) =>
        LookupSession(sessions, operatorId) switch
        {
            LookupResult<NeuralSession>.Found f =>
                new Result<NeuralSession, NeuralError>.Success(f.Value),
            LookupResult<NeuralSession>.NotFound nf =>
                new Result<NeuralSession, NeuralError>.Failure(
                    new NeuralError(ErrorCode.OperatorNotFound, $"Operator '{nf.Key}' was not found in telemetry logs.")),
            _ => throw new InvalidOperationException("Unknown lookup result.")
        };

    // checks if the risk score is too high or invalid
    public static Result<RiskAssessment, NeuralError> EvaluateSafeRisk(
        ValidatedNeuralSession session,
        Func<NeuralSession, double> policy)
    {
        var raw = session.ToRaw();
        double score = policy(raw);

        if (!double.IsFinite(score) || score >= RiskPolicies.CriticalBreakdownLimit)
        {
            return new Result<RiskAssessment, NeuralError>.Failure(
                new NeuralError(
                    ErrorCode.MentalBreakdown,
                    $"Mental Breakdown for {session.OperatorId} ({session.OperatorName}): Risk {score} >= {RiskPolicies.CriticalBreakdownLimit} limit ({RiskPolicies.ExplainReason(raw, score)})."));
        }

        return new Result<RiskAssessment, NeuralError>.Success(
            new RiskAssessment(
                session.OperatorId,
                score,
                session.OperatorName,
                session.ImplantModel,
                RiskPolicies.ExplainReason(raw, score)));
    }

    // finds the operator and checks their risk using bind and map
    public static Result<OperatorStatus, NeuralError> DiagnoseOperator(
        IReadOnlyList<NeuralSession> sessions,
        string operatorId,
        Func<NeuralSession, double> policy) =>
        RequireSession(sessions, operatorId)
            .Bind(SessionFactory.CreateResult)
            .Bind(valid => EvaluateSafeRisk(valid, policy))
            .Map(assessment => new OperatorStatus(
                assessment.SessionId,
                assessment.OperatorName,
                assessment.ImplantModel,
                assessment.RiskScore,
                RiskPolicies.ClassifyStatus(assessment.RiskScore),
                assessment.Reason));

    // loads sessions from a json file and handles file errors
    public static Result<IReadOnlyList<NeuralSession>, NeuralError> LoadSessionsFromJson(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure(
                new NeuralError(ErrorCode.FileNotFound, $"Telemetry file not found at '{filePath}'."));
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<List<NeuralSession>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data is null)
            {
                return new Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure(
                    new NeuralError(ErrorCode.MalformedFile, $"File '{filePath}' contained null telemetry data."));
            }

            return new Result<IReadOnlyList<NeuralSession>, NeuralError>.Success(data.AsReadOnly());
        }
        catch (JsonException ex)
        {
            return new Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure(
                new NeuralError(ErrorCode.MalformedFile, $"Malformed JSON in '{filePath}': {ex.Message}"));
        }
        catch (IOException ex)
        {
            return new Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure(
                new NeuralError(ErrorCode.FileNotFound, $"I/O error reading '{filePath}': {ex.Message}"));
        }
    }
}
