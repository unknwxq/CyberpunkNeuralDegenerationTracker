# SynapseSec
## 1. About the project
The project works with cyberpunk neural session data, and calculates risk scores for operators so they dont go full cyberpsycho in middle of work[cite: 3].
In Assignment 5 we built pure functional pipeline with two policies Safe-Net and Overclock[cite: 3, 7].
In Assignment 6 we fixed temporal coupling with state machine (NotReady, Ready, Completed) and SessionFactory so nobody calculates risk before validation[cite: 5].
In Assignment 7 we added functional error handling with Result, LookupResult for missing operators, Map and Bind chaining, and safe JSON loading on boundary[cite: 6].
Now in Assignment 8 we upgraded the project with custom extension methods, curried ripperdoc functions, two analytical LINQ reports, lazy streaming with yield return, safe file streams with using, and a real native C library cybersignal.dll called through P/Invoke[cite: 8]

Also we cleaned up Program.cs by moving all big console printing methods into seperate file ConsolePrinter.cs, so Program.cs is now super short and clean

## 2. Custom extension methods (Task 1)
In RiskPolicies.cs we implemented two custom extension methods and used both in the main program[cite: 8]:
- GetRipperdocAdvice(this NeuralSession s) - receiver type is NeuralSession, and return type is string[cite: 8]. It checks operator GHz load and Overclock score, then returns cyberpunk medical advice like calling MaxTac Psycho Squad, injecting Viktor immunoblockers, or doing nothing if chrome is stable. It belongs on NeuralSession because medical advice depends directly on single operator session metrics[cite: 8].
- KeepSuccesses(this IEnumerable of Result) - receiver type is IEnumerable of Result<T, E>, and return type is IReadOnlyList<T>[cite: 8]. It loops through a batch of Result items, keeps only Success values, and skips all Failure items. It belongs on IEnumerable of Result because filtering successful outcomes from batch diagnostics is a reusable collection operation[cite: 8]

## 3. Two LINQ reports (Task 2)
We created LinqReports.cs to generate two different analytical reports from sessions.json data without mixing calculations with Console output[cite: 8]:

### Report 1: BuildImplantThreatReport
- Input and user: takes collection of NeuralSession and risk policy delegate, intended for Cyberware Safety Auditors[cite: 3, 8].
- Operators used: uses Where to filter out empty implant names, GroupBy to group sessions by ImplantModel, Select to calculate SessionCount with Count(), TotalRisk with Sum(), and AverageGhz with Average()[cite: 3, 8]. Then it sorts with OrderByDescending by TotalRisk, ThenByDescending by SessionCount, and ThenBy by ImplantModel as tie-breaker so order is always deterministic[cite: 3, 8].
- Expected result: Militech Sandevistan-X (NextGen-Beta) is #1 most dangerous implant with 2 cases, Total Risk 315.0, and Avg GHz 5.50.

### Report 2: BuildMaxTacWatchlist
- Input and user: takes sessions, policy, and minRiskThreshold (default 50.0), intended for MaxTac and Trauma Team dispatchers[cite: 3, 8].
- Operators used: uses Select to project session with calculated score, Where to filter only dangerous operators with score >= 50, Select to build MaxTacWatchlistItem with GetRipperdocAdvice(), and sorts with OrderByDescending by RiskScore and ThenBy by OperatorId as tie-breaker when two operators have exact same risk score[cite: 8].
- Expected result: returns 4 high-risk operators (David Martinez with 180.0, Rebecca with 154.0, Songbird with 135.0, and V with 70.0) and skips safe operators like Panam, Lucy, and Kiwi

## 4. Currying and partial application (Task 3)
In RiskPolicies.cs we made a curried function CreateRipperdocScorer with signature:
double blockerEffect -> double ghzPenalty -> NeuralSession -> double[cite: 8]

It takes arguments one by one in chain[cite: 8]. Using partial application we fixed different clinic parameters and got two configured functions[cite: 8]:
- ViktorVektorClinic = CreateRipperdocScorer(0.7)(8.0) - Viktor uses quality immunoblockers that reduce stress by 30% and put low penalty on extra GHz.
- FingersShadyClinic = CreateRipperdocScorer(1.2)(15.0) - Fingers uses broken black-market parts that increase stress by 20% and punish extra GHz super hard.

On the exact same input (David Martinez NC-03), ViktorVektorClinic gives 108.0 risk, while FingersShadyClinic gives 189.0 risk[cite: 8]

## 5. Lazy processing with yield return and using resources (Tasks 4, 5, 6)
### Deferred execution (Task 4)
In LinqReports.StreamEvaluatedSessions() we implemented a lazy generator using yield return that validates sessions and creates OperatorStatus items one by one[cite: 3, 8].
Creating the query does zero work (counter is 0)[cite: 8]. Processing only runs when we call ToList() to materialise a snapshot[cite: 8]. Before ToList(), enumerating the query multiple times would re-run validation and policy math every single time[cite: 8]. After ToList(), we have a stable in-memory snapshot that can be reused without recalculating or touching files again[cite: 8].

### Safe resource disposal (Task 5)
In LinqReports.LoadSnapshotWithUsing() we open sessions.json using FileStream and StreamReader with the using keyword[cite: 8]. In ExportReportWithUsing() we save maxtac_watchlist.json using FileStream and StreamWriter with using[cite: 8].
The methods themselves own these streams, and the release point is the end of the try block scope[cite: 8]. Even if JsonException or IOException happens, using guarantees that file handles are closed immediately instead of waiting for garbage collector[cite: 8].

### Memory retention and allocation note (Task 6)
Datasets are materialised into arrays or read-only lists at clear boundaries: LoadSnapshotWithUsing(), ToArray() at the end of both LINQ reports, and ToList() on the yield stream in ConsolePrinter[cite: 8].
Our deferred query in StreamEvaluatedSessions holds a reference to the input sessions collection and policy delegate inside its compiler-generated iterator closure until enumeration finishes[cite: 8].
We do not use any long-lived static caches, growing global lists, or event subscriptions that could cause memory leaks[cite: 8]. The Implants dictionary in RiskPolicies is a fixed read-only table of 5 items.
To avoid unnecessary retention and repeated file allocation, LoadSnapshotWithUsing reads the JSON file inside a short using scope and returns an in-memory snapshot, so we never keep a FileStream open while running LINQ queries[cite: 8]. Streaming with yield return uses less RAM on huge logs, while keeping a ToList() snapshot is better for small datasets that we need to print and inspect multiple times[cite: 3, 8]

## 6. Native C library integration (Tasks 7, 8, 9)
We wrote a small native C library in SynapseSec/Native/cybersignal.c and connected it to C# via P/Invoke in CyberSignalNative.cs[cite: 8].

### Native function contract
- Function name: calc_humanity_index[cite: 8]
- Input types: int stress_level (valid 0..10), int ghz_load (valid >= 0), int blocker_doses (valid >= 0)[cite: 8]
- Output type: int representing operator Humanity Stability Index from 0 to 100 percent[cite: 8]
- Invalid input behaviour: if stress_level < 0 or > 10, or ghz_load < 0, or blocker_doses < 0, C function immediately returns -1[cite: 8].
- Formula and manual example: 100 - (stress_level * 6) - (ghz_load * 5) + (blocker_doses * 6), clamped between 0 and 100[cite: 8]. For stress = 5, ghz = 4, and blocker_doses = 2: 100 - 30 - 20 + 12 = 62% humanity[cite: 8].
- Memory ownership: the function uses only simple value-type integers on the CPU stack and does not allocate heap memory with malloc, so no manual free is needed[cite: 8].

### Platform, compiler, and build instructions
- OS and Architecture: Windows 10/11 x64 (64-bit)[cite: 8]
- Compiler: GCC (MinGW-w64)[cite: 8]
- Exact build command from project root[cite: 8]:

    gcc -shared -O2 .\SynapseSec\Native\cybersignal.c -o .\SynapseSec\Native\cybersignal.dll

- Binary location and loading: the compiled binary is placed at SynapseSec/Native/cybersignal.dll[cite: 8]. In SynapseSec.csproj we configured CopyToOutputDirectory so MSBuild automatically copies cybersignal.dll and sessions.json into bin/Debug/net10.0/ right next to SynapseSec.exe[cite: 8].
- C# Binding and Adapter: CyberSignalNative.cs uses [DllImport("cybersignal", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)][cite: 8]. The adapter CheckHumanityStability() validates C# inputs first, calls the C function, and catches DllNotFoundException, EntryPointNotFoundException, or BadImageFormatException, returning Result.Failure with ErrorCode.NativeCallFailed so the app never crashes[cite: 8].
- Integration difficulty and resolution: at first when running dotnet run, .NET could not find cybersignal.dll because it stayed inside the Native source folder instead of the bin output folder[cite: 8]. We fixed this by adding a None Include item with Link and CopyToOutputDirectory in SynapseSec.csproj so the DLL is always copied to the output folder on build[cite: 8]

## 7. Main files
### Models
- Result.cs - Result<T, E> with Map and Bind, LookupResult<T>, NeuralError, and ErrorCode (including NativeCallFailed)[cite: 6, 8].
- NeuralSession.cs & ValidatedNeuralSession.cs - raw and factory-validated session models[cite: 5].
- SessionValidationResult.cs & WorkflowOperationResult.cs - union models for Assignment 6[cite: 5].
- RiskAssessment.cs & OperatorStatus.cs - output models with risk score, status, and reason[cite: 3, 6].
- SampleData.cs - fallback dataset with 7 Cyberpunk characters.

### Pipeline
- LinqReports.cs - BuildImplantThreatReport(), BuildMaxTacWatchlist(), StreamEvaluatedSessions() with yield return, LoadSnapshotWithUsing(), and ExportReportWithUsing()[cite: 8].
- ConsolePrinter.cs - handles all console output for Assignments 5, 6, 7, and 8[cite: 4].
- RiskPipeline.cs - Run(), LookupSession(), RequireSession(), EvaluateSafeRisk(), DiagnoseOperator(), and LoadSessionsFromJson()[cite: 6, 7].
- SessionFactory.cs & RiskAssessmentWorkflow.cs - validation factory and state machine[cite: 5].
- SessionValidationStage.cs & RiskReportStage.cs - pipeline stages from Assignment 5[cite: 7].

### Policies & Native
- RiskPolicies.cs - Implants dictionary, SafeNet, Overclock, CreateRipperdocScorer curried function, ViktorVektorClinic, FingersShadyClinic, GetRipperdocAdvice(), and KeepSuccesses()[cite: 7, 8].
- RiskCalculator.cs - Compose extension and ToRaw converter[cite: 7].
- Native/cybersignal.c & Native/CyberSignalNative.cs - native C source, DLL, and P/Invoke Result adapter[cite: 8].
- sessions.json - JSON data file loaded on startup[cite: 6, 8].

## 8. Automated tests (13 cumulative tests)
In SynapseSec.Tests/UnitTest1.cs we have 13 comprehensive tests (all passing, 0 failed) covering Assignments 5 to 8[cite: 4, 8]:
1. NormalWorkflow_And_TwoPolicies_ProduceExpectedResults - checks SafeNet (64) vs Overclock (70)[cite: 7].
2. Workflow_EnforcesSafeOrder_And_RejectsInvalidStates - checks state machine NotReady, Ready, Completed[cite: 5].
3. CompleteSuccess_IntegratedResultChain_ProducesOperatorStatus - checks DiagnoseOperator for Lucy (40.0)[cite: 6].
4. DomainFailures_AllThreeDistinctFailures_ReturnExpectedErrorCodes - checks BrainwaveCorrupted, ImplantNotRecognized, and MentalBreakdown[cite: 3, 6].
5. Lookup_PresentZeroValuesAndAbsentOperator_AreHandledExplicitly - proves Kiwi (0 risk) is Found while NC-999 is OperatorNotFound[cite: 6].
6. FailurePropagation_SkipsSubsequentMapAndBindFunctions - uses bool flag to prove Map and Bind skip functions after Failure[cite: 6].
7. FileBoundary_MissingAndMalformedFiles_ReturnSafeFailures - checks FileNotFound and MalformedFile on temp files[cite: 6, 8].
8. CustomExtensions_GetRipperdocAdvice_And_KeepSuccesses_WorkCorrectly - tests both custom extension methods[cite: 8].
9. CurriedRipperdocScorer_PartialApplications_ProduceDifferentScoresOnSameInput - proves ViktorVektorClinic and FingersShadyClinic return different scores[cite: 8].
10. LinqReports_HandleNormal_Empty_NoMatches_And_TiedOrdering - checks grouping, totals, empty dataset, no matches, and ThenBy tie-breaker on NC-A vs NC-B[cite: 8].
11. DeferredExecution_YieldStream_DoesNotRunUntilEnumerated - uses counter probe to prove yield stream is 0 before ToList() and 7 after[cite: 8].
12. ResourceLifetime_ExportAndLoadWithUsing_ReleasesFileHandleImmediately - proves StreamWriter and StreamReader with using release file locks right away[cite: 8].
13. NativeIntegration_CalculatesExpectedHumanity_And_HandlesFailuresSafely - tests humanity calculation (62%) and NativeCallFailed error on negative input[cite: 8]

## 9. How to build and run
The project targets .NET 10 SDK on Windows x64[cite: 8].
Open PowerShell in the project root directory (where SynapseSec.slnx is located)[cite: 8]:

1. Compile native C library (optional if cybersignal.dll is already built in Native folder)[cite: 8]:

    gcc -shared -O2 .\SynapseSec\Native\cybersignal.c -o .\SynapseSec\Native\cybersignal.dll

2. Build the solution[cite: 8]:

    dotnet build SynapseSec.slnx

3. Run all 13 automated tests[cite: 8]:

    dotnet test SynapseSec.slnx

4. Run the console application[cite: 8]:

    dotnet run --project .\SynapseSec\SynapseSec.csproj

## 10. Individual contribution
Group: IT-2513
Student: Yeraly Tanzharyk - Lead Developer
Project ownership, Cyberpunk lore ideas (Viktor vs Fingers clinics, MaxTac watchlist, Humanity Index), and overall architecture.
Implemented custom extension methods, curried ripperdoc scorer, and both LINQ reports with tie-breakers in LinqReports.cs[cite: 8].
Implemented lazy yield return streaming, safe FileStream/StreamReader/StreamWriter disposal with using, and ConsolePrinter separation[cite: 8].
Wrote cybersignal.c in C, compiled cybersignal.dll with GCC, and built CyberSignalNative P/Invoke Result adapter[cite: 8].
Wrote all automated tests in UnitTest1.cs and prepared this documentation[cite: 8].

Supporting AI Contributors

ChatGPT
Assisted with explaining currying, deferred execution, and why P/Invoke needs Cdecl calling convention.

Claude
Helped clean up Program.cs and move console formatting into ConsolePrinter.cs.

Google Gemini
Contributed ideas for LINQ reports, Cyberpunk ripperdoc clinics, and native C humanity formula.

GitHub Copilot and Cursor AI
Offered autocomplete suggestions so I dont die typing boilerplate code.

Microsoft Learn and Stack Overflow Community
Honorary consulting department for .NET 10 P/Invoke docs and GCC shared library flags.

I worked on the project individually all by myself with my AI besties like one lead developer managing several AI interns. My work included implementing all functional stages from Assignment 5 to 8, LINQ reports, lazy streams, native C integration, automated tests, and documentation[cite: 4, 8].