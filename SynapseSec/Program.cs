using SynapseSec.Models;
using SynapseSec.Pipeline;

// loads sessions from json file, or falls back to sample data
string jsonPath = Path.Combine(AppContext.BaseDirectory, "sessions.json");
var sessions = LinqReports.LoadSnapshotWithUsing(jsonPath) is Result<IReadOnlyList<NeuralSession>, NeuralError>.Success loaded
    ? loaded.Value
    : SampleData.CreateSessions();

// runs each assignment demonstration from separate printer class
ConsolePrinter.RunAssignment5(sessions);
ConsolePrinter.RunAssignment6(sessions);
ConsolePrinter.RunAssignment7(sessions);
ConsolePrinter.RunAssignment8(sessions);