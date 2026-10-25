
namespace SynapseSec.Models;

//stores result of a risk calculation
public sealed record RiskAssessment(
    string SessionId,
    double RiskScore,
    string OperatorName = "Unknown",
    string ImplantModel = "",
    string Reason = "");
