
using SynapseSec.Models;

namespace SynapseSec.Policies;

// stores implant info and its safe limits
public sealed record ImplantSpec(string Tier, double MaxSafeGhz, double Multiplier);

public static class RiskPolicies
{
    public const double CriticalBreakdownLimit = 150.0;

    public static readonly IReadOnlyDictionary<string, ImplantSpec> Implants =
        new Dictionary<string, ImplantSpec>(StringComparer.OrdinalIgnoreCase)
        {
            ["Zetatech Bio-Link (Budget)"]          = new("Budget",       MaxSafeGhz: 3.0, Multiplier: 1.2),
            ["Arasaka Mk.IV (Corporate)"]           = new("Corporate",    MaxSafeGhz: 4.0, Multiplier: 1.0),
            ["Biotechnica NeuraLux (Exclusive)"]    = new("Exclusive",    MaxSafeGhz: 6.0, Multiplier: 0.8),
            ["Maelstrom Ripper-Mod (Black-Market)"] = new("Black-Market", MaxSafeGhz: 3.5, Multiplier: 1.4),
            ["Militech Sandevistan-X (NextGen-Beta)"] = new("NextGen-Beta", MaxSafeGhz: 4.5, Multiplier: 1.5)
        };

    // gets implant info or uses default values if not found
    public static ImplantSpec GetSpec(string model) =>
        Implants.TryGetValue(model?.Trim() ?? "", out var spec)
            ? spec
            : new ImplantSpec("Standard", MaxSafeGhz: 4.0, Multiplier: 1.0);

    // calculates risk from stress and session time
    public static double SafeNet(NeuralSession s)
    {
        var spec = GetSpec(s.ImplantModel);
        return Math.Round((10 * s.StressLevel + 2 * s.SessionDurationHours) * spec.Multiplier, 1);
    }

    // calculates risk when neural load is above the safe limit
    public static double Overclock(NeuralSession s)
    {
        var spec = GetSpec(s.ImplantModel);
        double overload = Math.Max(0, s.NeuralLoadGhz - spec.MaxSafeGhz);
        return Math.Round((10 * s.StressLevel + 10 * overload * overload) * spec.Multiplier, 1);
    }

    // returns status based on risk score
    public static string ClassifyStatus(double score) => score switch
    {
        >= CriticalBreakdownLimit => "CYBERPSYCHOSIS BREAKDOWN (Emergency Purge!)",
        >= 100                    => "CRITICAL OVERLOAD (Mandatory Disconnect!)",
        >= 50                     => "WARNING (Elevated Neural Strain)",
        _                         => "NORMAL (Stable Brainwaves)"
    };

    // explains why the operator has this risk score
    public static string ExplainReason(NeuralSession s, double score)
    {
        var spec = GetSpec(s.ImplantModel);
        bool isOverclocked = s.NeuralLoadGhz > spec.MaxSafeGhz;

        if (isOverclocked && s.StressLevel >= 7)
            return $"Severe stress ({s.StressLevel}/10) + load ({s.NeuralLoadGhz} GHz > {spec.MaxSafeGhz} GHz {spec.Tier} limit, x{spec.Multiplier})";

        if (isOverclocked)
            return $"Neural load ({s.NeuralLoadGhz} GHz) exceeds {spec.Tier} safe limit ({spec.MaxSafeGhz} GHz, x{spec.Multiplier})";

        if (s.StressLevel >= 7)
            return $"High stress ({s.StressLevel}/10) amplified by {spec.Tier} implant (x{spec.Multiplier})";

        if (s.SessionDurationHours >= 5)
            return $"Extended session ({s.SessionDurationHours}h) on {spec.Tier} chip (x{spec.Multiplier})";

        return $"Within {spec.Tier} safe limits (load <= {spec.MaxSafeGhz} GHz, multiplier x{spec.Multiplier})";
    }
}
