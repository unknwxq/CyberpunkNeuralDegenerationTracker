
namespace SynapseSec.Models;

//session that has passed validation properties cant change after creation
public sealed class ValidatedNeuralSession
{
    public string OperatorId { get; }
    public string ImplantModel { get; }
    public double SessionDurationHours { get; }
    public double NeuralLoadGhz { get; }
    public double StressLevel { get; }

    internal ValidatedNeuralSession(
        string operatorId,
        string implantModel,
        double sessionDurationHours,
        double neuralLoadGhz,
        double stressLevel)
    {
        OperatorId = operatorId;
        ImplantModel = implantModel;
        SessionDurationHours = sessionDurationHours;
        NeuralLoadGhz = neuralLoadGhz;
        StressLevel = stressLevel;
    }
}
