
using SynapseSec.Models;

namespace SynapseSec.Policies;

public static class RiskCalculator
{
    //only validated sessions can enter the new calculation path
    public static double Calculate(
        ValidatedNeuralSession session,
        Func<ValidatedNeuralSession, double> policy)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(policy);

        return policy(session);
    }
}
