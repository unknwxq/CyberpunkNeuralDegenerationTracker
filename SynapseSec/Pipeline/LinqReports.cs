using System.Text.Json;
using SynapseSec.Models;
using SynapseSec.Policies;

namespace SynapseSec.Pipeline;

// model for linq report 1: groups sessions by implant model
public sealed record ImplantThreatReportItem(
    string ImplantModel,
    string Tier,
    int SessionCount,
    double TotalRisk,
    double AverageGhz);

// model for linq report 2: shows operators who need maxtac or blockers
public sealed record MaxTacWatchlistItem(
    string OperatorId,
    string OperatorName,
    double RiskScore,
    string Advice);

public static class LinqReports
{
    // linq report 1: groups by implant, calculates sum and average, sorts with tie breaker
    public static IReadOnlyList<ImplantThreatReportItem> BuildImplantThreatReport(
        IEnumerable<NeuralSession> sessions,
        Func<NeuralSession, double> policy)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(policy);

        return sessions
            .Where(s => !string.IsNullOrWhiteSpace(s.ImplantModel))
            .GroupBy(s => s.ImplantModel.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ImplantThreatReportItem(
                ImplantModel: g.Key,
                Tier: RiskPolicies.GetSpec(g.Key).Tier,
                SessionCount: g.Count(),
                TotalRisk: Math.Round(g.Sum(s => policy(s)), 1),
                AverageGhz: Math.Round(g.Average(s => s.NeuralLoadGhz), 2)))
            .OrderByDescending(r => r.TotalRisk)
            .ThenByDescending(r => r.SessionCount)
            .ThenBy(r => r.ImplantModel, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    // linq report 2: filters high risk operators, projects advice, orders with tie breaker
    public static IReadOnlyList<MaxTacWatchlistItem> BuildMaxTacWatchlist(
        IEnumerable<NeuralSession> sessions,
        Func<NeuralSession, double> policy,
        double minRiskThreshold = 50.0)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(policy);

        return sessions
            .Select(s => new { Session = s, Score = policy(s) })
            .Where(x => x.Score >= minRiskThreshold)
            .Select(x => new MaxTacWatchlistItem(
                x.Session.OperatorId.Trim(),
                x.Session.OperatorName.Trim(),
                x.Score,
                x.Session.GetRipperdocAdvice()))
            .OrderByDescending(x => x.RiskScore)
            .ThenBy(x => x.OperatorId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    // lazy processing with yield return: runs only when foreach or ToList is called
    public static IEnumerable<OperatorStatus> StreamEvaluatedSessions(
        IEnumerable<NeuralSession> sessions,
        Func<NeuralSession, double> policy,
        Action? onStepExecuted = null)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(policy);

        foreach (var s in sessions)
        {
            onStepExecuted?.Invoke();
            if (SessionFactory.Create(s) is SessionValidationResult.Valid { Session: var valid })
            {
                var raw = valid.ToRaw();
                double score = policy(raw);
                yield return new OperatorStatus(
                    raw.OperatorId,
                    raw.OperatorName,
                    raw.ImplantModel,
                    score,
                    RiskPolicies.ClassifyStatus(score),
                    raw.GetRipperdocAdvice());
            }
        }
    }

    // reads json lines or file stream with using, so file handle is always closed safely
    public static Result<IReadOnlyList<NeuralSession>, NeuralError> LoadSnapshotWithUsing(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return new Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure(
                new NeuralError(ErrorCode.FileNotFound, $"File not found: '{filePath}'."));

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();

            var data = JsonSerializer.Deserialize<List<NeuralSession>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return data is not null
                ? new Result<IReadOnlyList<NeuralSession>, NeuralError>.Success(data.AsReadOnly())
                : new Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure(
                    new NeuralError(ErrorCode.MalformedFile, "JSON content was null."));
        }
        catch (JsonException ex)
        {
            return new Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure(
                new NeuralError(ErrorCode.MalformedFile, $"Broken JSON: {ex.Message}"));
        }
        catch (IOException ex)
        {
            return new Result<IReadOnlyList<NeuralSession>, NeuralError>.Failure(
                new NeuralError(ErrorCode.FileNotFound, $"IO failure: {ex.Message}"));
        }
    }

    // exports report to disk using StreamWriter, and closes stream automatically
    public static Result<string, NeuralError> ExportReportWithUsing<T>(string filePath, IReadOnlyList<T> report)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            writer.Write(json);
            return new Result<string, NeuralError>.Success(filePath);
        }
        catch (Exception ex)
        {
            return new Result<string, NeuralError>.Failure(
                new NeuralError(ErrorCode.FileNotFound, $"Could not export report: {ex.Message}"));
        }
    }
}