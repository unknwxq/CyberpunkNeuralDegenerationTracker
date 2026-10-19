
using SynapseSec.Models;

namespace SynapseSec.Policies;

// this for calculating neural session risk
public static class RiskPolicies
{

    public static double SafeNet(NeuralSession session)
    {
        return 10 * session.StressLevel
             + 2 * session.SessionDurationHours;
    }


    public static double Overclock(NeuralSession session)
    {
        double overload = Math.Max(0, session.NeuralLoadGhz - 4);

        return 10 * session.StressLevel
             + 10 * overload * overload;
    }
}
