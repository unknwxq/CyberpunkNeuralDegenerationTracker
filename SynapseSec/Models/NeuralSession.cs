namespace SynapseSec.Models;

public class NeuralSession
{
    public string OperatorId { get; set; } = string.Empty;
    public string OperatorName { get; set; } = "Unknown"; 
    public string ImplantModel { get; set; } = string.Empty;
    public double SessionDurationHours { get; set; }
    public double NeuralLoadGhz { get; set; }
    public double StressLevel { get; set; }
}