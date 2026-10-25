namespace SynapseSec.Models;

public static class SampleData
{
    public static IReadOnlyList<NeuralSession> CreateSessions() => Array.AsReadOnly(
    [
        new NeuralSession
        {
            OperatorId = "NC-01",
            OperatorName = "V (Valerie)",
            ImplantModel = "Arasaka Mk.IV (Corporate)",
            StressLevel = 6,
            SessionDurationHours = 2,
            NeuralLoadGhz = 5.0
        },
        new NeuralSession
        {
            OperatorId = "NC-02",
            OperatorName = "Lucy Kushinada",
            ImplantModel = "Biotechnica NeuraLux (Exclusive)",
            StressLevel = 4,
            SessionDurationHours = 5,
            NeuralLoadGhz = 5.5
        },
        new NeuralSession
        {
            OperatorId = "NC-03",
            OperatorName = "David Martinez",
            ImplantModel = "Militech Sandevistan-X (NextGen-Beta)",
            StressLevel = 8,
            SessionDurationHours = 2,
            NeuralLoadGhz = 6.5
        },
        new NeuralSession
        {
            OperatorId = "NC-04",
            OperatorName = "Rebecca",
            ImplantModel = "Maelstrom Ripper-Mod (Black-Market)",
            StressLevel = 7,
            SessionDurationHours = 1,
            NeuralLoadGhz = 5.5
        },
        new NeuralSession
        {
            OperatorId = "NC-05",
            OperatorName = "Panam Palmer",
            ImplantModel = "Zetatech Bio-Link (Budget)",
            StressLevel = 3,
            SessionDurationHours = 4,
            NeuralLoadGhz = 3.0
        },
        new NeuralSession
        {
            OperatorId = "NC-06",
            OperatorName = "Kiwi",
            ImplantModel = "Arasaka Mk.IV (Corporate)",
            StressLevel = 0,
            SessionDurationHours = 0,
            NeuralLoadGhz = 0
        },
        new NeuralSession
        {
            OperatorId = "NC-07",
            OperatorName = "Songbird (So Mi)",
            ImplantModel = "Militech Sandevistan-X (NextGen-Beta)",
            StressLevel = 9,
            SessionDurationHours = 6,
            NeuralLoadGhz = 4.5
        }
    ]);
}