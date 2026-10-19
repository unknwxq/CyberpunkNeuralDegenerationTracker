
namespace SynapseSec.Models;

public static class SampleData
{
    public static IReadOnlyList<NeuralSession> CreateSessions()
    {
        return Array.AsReadOnly(new[]
        {
            new NeuralSession
            {
                OperatorId = "OP-01",
                ImplantModel = "Neuro-X",
                StressLevel = 6,
                SessionDurationHours = 2,
                NeuralLoadGhz = 5
            },
            new NeuralSession
            {
                OperatorId = "OP-02",
                ImplantModel = "Neuro-A",
                StressLevel = 3,
                SessionDurationHours = 1,
                NeuralLoadGhz = 4
            },
            new NeuralSession
            {
                OperatorId = "OP-03",
                ImplantModel = "Neuro-X",
                StressLevel = 8,
                SessionDurationHours = 0,
                NeuralLoadGhz = 6
            },
            new NeuralSession
            {
                OperatorId = "OP-04",
                ImplantModel = "Neuro-B",
                StressLevel = 2,
                SessionDurationHours = 3,
                NeuralLoadGhz = 2
            },
            new NeuralSession
            {
                OperatorId = "OP-05",
                ImplantModel = "Neuro-A",
                StressLevel = 5,
                SessionDurationHours = 0,
                NeuralLoadGhz = 4
            },
            new NeuralSession
            {
                OperatorId = "OP-06",
                ImplantModel = "Neuro-X",
                SessionDurationHours = 0,
                NeuralLoadGhz = 0,
                StressLevel = 0
            },
            new NeuralSession
            {
                OperatorId = "OP-07",
                ImplantModel = "Neuro-X",
                SessionDurationHours = 8,
                NeuralLoadGhz = 4,
                StressLevel = 10
            },
            new NeuralSession
            {
                OperatorId = "OP-08",
                ImplantModel = "Neuro-Pro",
                SessionDurationHours = 2,
                NeuralLoadGhz = 6,
                StressLevel = 3
            },
            new NeuralSession
            {
                OperatorId = "OP-09",
                ImplantModel = "Neuro-Pro",
                SessionDurationHours = 5,
                NeuralLoadGhz = 5,
                StressLevel = 0
            }

        });
    }
}
