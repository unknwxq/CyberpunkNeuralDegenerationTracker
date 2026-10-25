
namespace SynapseSec.Models;

// stores session data after validation
public sealed class ValidatedNeuralSession
{
    public string OperatorId { get; }
    public string OperatorName { get; }
    public string ImplantModel { get; }
    public double SessionDurationHours { get; }
    public double NeuralLoadGhz { get; }
    public double StressLevel { get; }

    // sets all session values when creating the object
    internal ValidatedNeuralSession(
        string operatorId,
        string operatorName,
        string implantModel,
        double sessionDurationHours,
        double neuralLoadGhz,
        double stressLevel)
    {
        OperatorId = operatorId;
        OperatorName = operatorName;
        ImplantModel = implantModel;
        SessionDurationHours = sessionDurationHours;
        NeuralLoadGhz = neuralLoadGhz;
        StressLevel = stressLevel;
    }
}
