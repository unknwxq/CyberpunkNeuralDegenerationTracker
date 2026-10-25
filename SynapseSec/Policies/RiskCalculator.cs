
using SynapseSec.Models;

namespace SynapseSec.Policies;

// methods for calculating risk
public static class RiskCalculator
{
    // combines two functions so one runs after another
    public static Func<A, C> Compose<A, B, C>(this Func<B, C> g, Func<A, B> f)
    {
        ArgumentNullException.ThrowIfNull(g);
        ArgumentNullException.ThrowIfNull(f);
        return x => g(f(x));
    }

    // converts validated session to a normal session
    public static NeuralSession ToRaw(this ValidatedNeuralSession s) => new()
    {
        OperatorId = s.OperatorId,
        OperatorName = s.OperatorName,
        ImplantModel = s.ImplantModel,
        SessionDurationHours = s.SessionDurationHours,
        NeuralLoadGhz = s.NeuralLoadGhz,
        StressLevel = s.StressLevel
    };

    // checks the inputs and runs the risk policy
    public static double Calculate(
        ValidatedNeuralSession session,
        Func<ValidatedNeuralSession, double> policy)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(policy);
        return policy(session);
    }
}
