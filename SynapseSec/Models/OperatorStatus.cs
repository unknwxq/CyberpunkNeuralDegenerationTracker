
namespace SynapseSec.Models;

// stores operator info and current risk status
public sealed record OperatorStatus(
    string OperatorId,
    string OperatorName,
    string ImplantModel,
    double RiskScore,
    string Status,
    string Reason);
