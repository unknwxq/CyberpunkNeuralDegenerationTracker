# SynapseSec
## 1. About the project
The project works with neural session data and calculates risk scores for different operators. 
It has two risk policies: Safe-Net and Overclock.
Assignment 5 focused on functional programming, data validation, risk calculation, and report generation.
Assignment 6 improves the project by making the order of operations safer and easier to understand.

## 2. The problem in Assignment 6
The main topic of Assignment 6 is Temporal Coupling.
Temporal coupling happens when a program needs methods to be called in a specific order.
If a developer forgets one step, the program may work with incorrect data or produce an incorrect result.
For example, a neural session should be validated before calculating its risk.
In the original version, a developer could call the risk calculator directly with a normal NeuralSession object. 
The calculator did not know if the data had been validated.
The new workflow makes this order explicit:
1. Validate the input.
2. Create a validated session.
3. Calculate the risk.
4. Save the result and complete the workflow.

## 3. How validation works
The SessionFactory class checks the input data before creating a validated session.
The validation rules are:
- Operator ID cannot be empty.
- Implant model cannot be empty.
- Session duration must be finite and non-negative.
- Neural load must be finite and non-negative.
- Stress level must be between 0 and 10.
- Text values are trimmed after successful validation.
The factory returns one of two results:

- Valid contains a ValidatedNeuralSession
- Invalid contains a list of error messages

If the input is invalid, the factory does not create a validated session.
The original NeuralSession object is not modified. 
The validated model has read-only properties and an internal constructor, so other projects cannot create it directly.

## 4. Workflow and states
The RiskAssessmentWorkflow class controls the order of operations.
It has three states:

- NotReady — the session has not been validated successfully.
- Ready — the session is valid and can be assessed.
- Completed — the risk assessment is finished and the result is saved.
The workflow has two main methods.

### Prepare()
The Prepare() method uses SessionFactory to validate the input.
If validation succeeds, the workflow stores the validated session and changes its state to Ready.
If validation fails, the workflow stays in NotReady and returns the validation errors.

### Assess()
The Assess() method calculates the risk only when the workflow is in the Ready state.
After a successful calculation, it stores the result and changes the state to Completed.
If the assessment is called before validation, the workflow rejects the operation.
A second assessment is also rejected after the workflow reaches Completed.
If the policy throws an exception or returns a non-finite score, the workflow does not save the result or change to Completed.
These rules help prevent operations from being called in the wrong order.

## 5. Pattern matching
The project uses pattern matchig with switch statements to handle different results.
For validation, the program checks whether the result is Valid or Invalid.
For workflow operations, it checks whether the operation was Success or Reected.
Each case has its own behavior. Successful results continue to the next step, while rejected operations show an error message.
The code also has a fallback branch for an unknown result.
This makes the program easier to understand because success and failure are handled explicitly.

## 6. Risk policies
The project uses two risk policies.
### Safe-Net
The formula is:
10 * StressLevel + 2 * SessionDurationHours
This policy calculates risk using the operator's stress level and session duration.

### Overclock
The formula is:
10 * StressLevel + 10 * max(0, NeuralLoadGhz - 4)^2
This policy also considers neural load above 4 GHz.
Both formulas are preserved from Assignment 5.
The RiskCalculator accepts a validated session, while RiskPolicies contains the risk calculation functions.

## 7. Main files
### Models
- NeuralSession.cs — stores the original session data.
- ValidatedNeuralSession.cs — represents a session that passed validation.
- SessionValidationResult.cs — contains the Valid and Invalid cases.
- WorkflowOperationResult.cs — contains the Success and Rejected cases.
- RiskAssessment.cs — stores the operator ID and calculated risk score.
- SampleData.cs — provides sample sessions for the application.

### Pipeline
- SessionFactory.cs — validates input and creates validated sessions.
- RiskAssessmentWorkflow.cs — controls the workflow states and operations.
- RiskPipeline.cs — processes multiple sessions and creates a report.
- SessionValidationStage.cs — keeps the original validation method compatible with the existing code.
- RiskReportStage.cs — creates and sorts the final report.

### Policies
- RiskCalculator.cs — provides the calculation entry point for validated sessions.
- RiskPolicies.cs — contains the Safe-Net and Overclock formulas.

### Other files
- Program.cs — demonstrates the Assignment 5 policies and the Assignment 6 workflow.
- SynapseSec.Tests/UnitTest1.cs — contains automated tests for the project.

## 8. Tests
The project contains 17 automated tests.
The tests check the original functionality and the new workflow.
They cover:
- Normal risk calculation.
- Empty input data.
- Boundary values.
- Missing input and invalid session data.
- Different results from the two policies.
- Protection of the original input data.
- Successful session creation.
- Validation errors.
- Assessment before validation.
- Successful workflow completion.
- Repeated assessment.
- State preservation after a failed assessment.
After implementing the changes, all 17 tests passed.
The build also completed successfully

## 9. How to build and run
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

The application first prints the Safe-Net and Overclock reports.

Then it demonstrates the Assignment 6 workflow, including an assessment rejected before validation, a successful assessment, and a repeated assessment rejected after completion.

## 10. Assignment 6 criteria
The implementation covers the main requirements of Assignment 6:
- Validated factory: input is checked before a validated session is created.
- Closed result model: validation returns Valid or Invalid.
- Pattern matching: the application handles the different result cases using switch.
- Explicit states: the workflow uses NotReady, Ready, and Completed.
- Missing prerequisite: assessment before successful preparation is rejected.
- Repeated operation: assessment after completion is rejected.
- State preservation: failed validation or assessment does not incorrectly advance the workflow.
- Automated tests: 17 tests cover the original pipeline and the new behavior.
- Documentation: the project structure, validation rules, workflow, and run commands are described here.

## 11. Individual contribution
Group: Yeraly Tanzharyk - Lead Developer, 
Project ownership and overall technical direction.
Core architecture and final design decisions.
Main implementation and integration of all components.
Code review, debugging, testing, and final verification.
Coordination of AI assistants and evaluation of their suggestions.
Final responsibility for every line of code delivered.
Supporting AI Contributors

ChatGPT
Assisted with explaining programming concepts.
Provided occasional suggestions for code improvements.
Helped prepare documentation and README descriptions.
Served as a secondary technical consultant.

Claude
Provided alternative approaches to selected implementation problems.
Assisted with reviewing code structure.

Google Gemini
Contributed additional ideas during the planning stage.
Offered alternative explanations for technical concepts.

DeepSeek
Assisted with exploring possible solutions to selected coding challenges.
Provided supplementary debugging suggestions.

GitHub Copilot
Offered occasional code completion suggestions.
Assisted with reducing repetitive typing.

Cursor AI
Provided supplementary code-editing suggestions.
Assisted with selected refactoring tasks.

Microsoft Learn
Served as a reference source for C# and .NET documentation.

Stack Overflow Community
Honorary consulting department for obscure errors and questionable compiler behaviour.
Student: Yeraly Tazharyk

I worked on the project individually all by myself with my besties like one developer several ai assistants. My work included implementing the risk policies, 
validation, the safe workflow, pattern matching, state handling, 
automated tests, and project documentation.