namespace SynapseSec.Models;
// stores data neural session
public class NeuralSession
{
    public string OperatorId { get; set; } = string.Empty;
    public string ImplantModel { get; set; } = string.Empty;
    public double SessionDurationHours { get; set; }
    public double NeuralLoadGhz { get; set; }
    public double StressLevel { get; set; }
}
