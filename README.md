# SynapseSec
## 1. About the project
The project works with cyberpunk neural session data and calculates risk scores for different operators so they dont go full cyberpsycho in middle of work.
In Assignment 5 we did functional programming pipeline, data validation, risk calculation, and report generation.
In Assignment 6 we fixed temporal coupling by making order of operations safe with state machine and factory.
Now in Assignment 7 we added functional error handling with Result, missing value lookup with LookupResult, function chaining with Map and Bind, and safe JSON file loading without crashing the whole program.

Also we cleaned up code, added Compose function to remove ugly copy paste lines, added real characters from Cyberpunk 2077 and Edgerunners, and made implant models actually matter in math calculations

## 2. What we changed in code and models
Before this update all operators had boring names like OP-01 and implant model was just useless text string. Now we upgraded it:
- Added OperatorName field so every ID belongs to real character like V, Lucy Kushinada, David Martinez, Rebecca, Panam Palmer, Kiwi, and Songbird.
- Added implant specifications in RiskPolicies.Implants dictionary. Every implant now has its own safe frequency limit MaxSafeGhz and risk Multiplier:
- Zetatech Bio-Link (Budget) - cheap plastic chip, safe limit is 3.0 GHz, multiplier is 1.2
- Arasaka Mk.IV (Corporate) - standard corporate chip, safe limit is 4.0 GHz, multiplier is 1.0
- Biotechnica NeuraLux (Exclusive) - luxury cyberware for rich people, safe limit is 6.0 GHz, multiplier is 0.8 because it cools down the brain
- Maelstrom Ripper-Mod (Black-Market) - shady street implant, safe limit is 3.5 GHz, multiplier is 1.4
- Militech Sandevistan-X (NextGen-Beta) - crazy military prototype, safe limit is 4.5 GHz, multiplier is 1.5 (David Martinez uses it and obviously gets cooked)

Also on console output the program now prints human text Reason so you can actually understand why risk score is high or normal instead of staring at random numbers.

## 3. The problem in Assignment 7
In normal C# code people just throw exceptions for every single problem. If input is bad or implant is wrong, program throws exception and dies. In functional programming throwing exceptions for normal business rules is bad idea because it breaks the pipeline flow.

In Assignment 7 we solved this problem:
1. Expected business errors do not throw exceptions anymore, they return Result.Failure with error code and message.
2. Real try-catch is used only at application boundary when reading JSON files from disk.
3. Steps are connected with Map and Bind so if error happens on step 1, next steps are automatically skipped and program does not do useless work

## 4. Explicit domain outcomes (Task 1)
We created closed abstract record Result in Result.cs with two cases:
- Success - contains valid value after operation
- Failure - contains NeuralError object with ErrorCode and text explanation

We implemented 3 different expected domain failures:
1. BrainwaveCorrupted - happens in SessionFactory.CreateResult() when session numbers are broken, for example negative duration hours, stress level above 10, or empty operator ID.
2. ImplantNotRecognized - happens in SessionFactory.CreateResult() when operator uses illegal uncertified cyberware that does not exist in our official RiskPolicies.Implants list (like Adam Smasher with Borg-Frame-9000).
3. MentalBreakdown - happens in RiskPipeline.EvaluateSafeRisk() when calculated risk score is 150 or higher. For example David Martinez on Overclock policy gets 180 risk score and system stops him with MentalBreakdown error before his brain melts the fuck down.

## 5. Missing values and LookupResult (Task 2)
When we search for operator by ID in RiskPipeline.LookupSession(), we do not return null because null causes bugs everywhere. Instead we created LookupResult with two cases:
- Found - carries the found NeuralSession object
- NotFound - carries the missing operator ID string

Then RequireSession() converts NotFound into Result.Failure with ErrorCode.OperatorNotFound.
One super important rule in Assignment 7 is that zero values are valid data, not missing data. In our sample data Kiwi (NC-06) has StressLevel = 0, SessionDurationHours = 0, and NeuralLoadGhz = 0. When we search for NC-06, LookupSession returns Found, calculates 0.0 risk score, and gives Success with NORMAL status. It never confuses zero numbers with missing person

## 6. Map and Bind composition (Task 3)
Inside Result.cs we wrote two functions to chain operations together:
- Bind - used when next function can also fail and returns another Result
- Map - used when next function is pure transformation and always succeeds

In RiskPipeline.DiagnoseOperator() we chained the whole operator diagnosis into one pipeline:
1. RequireSession() finds operator by ID or returns OperatorNotFound failure.
2. Bind(SessionFactory.CreateResult) validates numbers and checks if implant is certified.
3. Bind(EvaluateSafeRisk) calculates risk score using policy and checks if score is below 150 limit.
4. Map() takes valid RiskAssessment and converts it into final OperatorStatus report with status label and text reason.

If any step returns Failure, Map and Bind just pass that Failure to the end and do not call the next functions at all.
Also we added Compose function in RiskCalculator.cs to combine ToRaw conversion with policy function so code in RiskPipeline is short and clean.

## 7. File boundary exception handling (Task 4)
Reading files from disk can fail for reasons outside our domain logic, so we put JSON loading in RiskPipeline.LoadSessionsFromJson() at the boundary:
- Missing file - if file path does not exist or IOException happens, it returns Result.Failure with FileNotFound code.
- Malformed file - if JSON file has broken syntax or null data, catch block catches JsonException and returns Result.Failure with MalformedFile code.
- Valid file - reads JSON, deserializes list of sessions, and returns Result.Success without breaking existing data in memory.

## 8. Risk policies math
### Safe-Net
The formula is:
(10 * StressLevel + 2 * SessionDurationHours) * ImplantMultiplier
This corporate policy checks stress and session duration hours, then multiplies by chip factor.

### Overclock
The formula is:
(10 * StressLevel + 10 * max(0, NeuralLoadGhz - ImplantMaxSafeGhz)^2) * ImplantMultiplier
This street policy ignores duration hours, but punishes operator hard if NeuralLoadGhz goes higher than safe GHz limit of their specific implant

## 9. Main files
### Models
- Result.cs - contains Result, LookupResult, NeuralError, and ErrorCode enum for Assignment 7.
- NeuralSession.cs - stores raw session data including OperatorName.
- ValidatedNeuralSession.cs - session that passed validation, has internal constructor.
- SessionValidationResult.cs - contains Valid and Invalid cases for Assignment 6.
- WorkflowOperationResult.cs - contains Success and Rejected cases for workflow.
- RiskAssessment.cs - stores operator ID, name, implant, score, and reason.
- OperatorStatus.cs - final status model created by Map in Assignment 7.
- SampleData.cs - sample sessions with Cyberpunk characters and implants.

### Pipeline
- SessionFactory.cs - Create() for Assignment 6 and CreateResult() for Assignment 7 validation.
- RiskAssessmentWorkflow.cs - controls workflow states NotReady, Ready, and Completed.
- RiskPipeline.cs - contains Run(), LookupSession(), RequireSession(), EvaluateSafeRisk(), DiagnoseOperator() chain, and LoadSessionsFromJson().
- SessionValidationStage.cs - normalizes session list.
- RiskReportStage.cs - creates and sorts final report by risk score.

### Policies
- RiskCalculator.cs - contains Compose method, ToRaw converter, and Calculate entry point.
- RiskPolicies.cs - contains Implants dictionary, SafeNet and Overclock formulas, status classifier, and reason generator.

### Other files
- Program.cs - runs demonstrations for Assignment 5, Assignment 6, and Assignment 7.
- SynapseSec.Tests/UnitTest1.cs - contains 25 automated tests.

## 10. Tests
The project now has 25 automated tests (17 from Assignments 5 and 6, plus 8 new tests for Assignment 7).
The Assignment 7 tests cover:
- Complete success chain through DiagnoseOperator for Lucy (NC-02).
- All 3 domain failures: BrainwaveCorrupted, ImplantNotRecognized, and MentalBreakdown.
- Present lookup with all zero values (Kiwi NC-06) and absent lookup (NC-999).
- Map and Bind executing functions on Success.
- Failure propagation test with bool flags proving that Map and Bind skip their functions when Result is Failure.
- Missing JSON file returning FileNotFound failure.
- Malformed JSON file returning MalformedFile failure using temporary file cleanup.
- Valid JSON file loading sessions properly.

All 25 tests pass with 0 errors.

## 11. How to build and run
The project targets .NET 10.
Open PowerShell in the project root directory.
Check the installed SDK:

    dotnet --version

Build the solution:

    dotnet build SynapseSec.slnx

Run the automated tests:

    dotnet test SynapseSec.slnx

Run the application:

    dotnet run --project .\SynapseSec\SynapseSec.csproj

First it prints Assignment 5 reports for Safe-Net and Overclock with character names and reasons.
Second it shows Assignment 6 state machine checks.
Third it shows Assignment 7 results: normal diagnosis, zero value lookup for Kiwi, 3 domain errors, missing operator error, valid JSON load, missing file error, and broken JSON error.

## 12. Individual contribution
Group: IT-2513
Student: Yeraly Tanzharyk - Lead Developer
Project ownership and overall technical direction.
Upgraded domain idea with Cyberpunk characters and unique implant weights.
Implemented Result, LookupResult, Map and Bind composition, and Compose function.
Implemented 3 domain errors, DiagnoseOperator pipeline, and JSON file boundary handling.
Wrote all 25 automated tests and updated documentation.
Final responsibility for every line of code delivered.

Supporting AI Contributors

ChatGPT
Assisted with explaining functional programming concepts and why throwing exceptions in pure functions is bad.
Helped with simple explanations.

Claude
Provided ideas for simplifying switch statements and cleaning up long methods.

Google Gemini
Contributed ideas for Assignment 7 Result chain, implant multipliers, and test cases.

DeepSeek
Assisted with debugging compiler errors when types did not want to match.

GitHub Copilot and Cursor AI
Offered code completion suggestions so my hands dont fall off from typing boilerplate shit.

Microsoft Learn and Stack Overflow Community
Honorary consulting department for .NET 10 documentation and weird compiler errors.

I worked on the project individually all by myself with my besties like one developer managing several AI assistants. My work included implementing the risk policies, implant tiers, validation factory, safe workflow, Result and Lookup models, Map and Bind pipeline, JSON file handling, automated tests, and project documentation.