
namespace SynapseSec.Models;

// stores risk analysis
public class OperatorStatus
{
    public string OperatorId { get; set; } = string.Empty;
    public double RiskScore { get; set; }
    public string Status { get; set; } = string.Empty;
}
