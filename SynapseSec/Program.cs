
using SynapseSec.Models;
using SynapseSec.Pipeline;
using SynapseSec.Policies;

var sessions = SampleData.CreateSessions();


Console.WriteLine(" Assignment 5: Risk Policies ");
Console.WriteLine();
Console.WriteLine("Safe-Net Policy");

var safeReport = RiskPipeline.Run(
    sessions,
    RiskPolicies.SafeNet);

foreach (var result in safeReport)
{
    Console.WriteLine(
        $"{result.SessionId}: Risk = {result.RiskScore}");
}

Console.WriteLine();
Console.WriteLine("Overclock Policy");

var overclockReport = RiskPipeline.Run(
    sessions,
    RiskPolicies.Overclock);

foreach (var result in overclockReport)
{
    Console.WriteLine(
        $"{result.SessionId}: Risk = {result.RiskScore}");
}

//demonstrate temporal coupling protection.
Console.WriteLine();
Console.WriteLine(" Assignment 6: Safe Workflow ");

var workflow = new RiskAssessmentWorkflow();

var earlyAssessment = workflow.Assess(
    session => 10 * session.StressLevel);

switch (earlyAssessment)
{
    case WorkflowOperationResult.Success success:
        Console.WriteLine(
            $"Unexpected success: {success.Assessment.RiskScore}");
        break;

    case WorkflowOperationResult.Rejected rejected:
        Console.WriteLine(
            $"Assessment before validation rejected: {rejected.Reason}");
        break;

    default:
        throw new InvalidOperationException(
            "Unknown workflow operation result.");
}

//prepare a session through the factory.
var preparation = workflow.Prepare(sessions[0]);

switch (preparation)
{
    case SessionValidationResult.Valid valid:
        Console.WriteLine(
            $"Validation successful: {valid.Session.OperatorId}");

        var assessment = workflow.Assess(
            session => 10 * session.StressLevel
                + 2 * session.SessionDurationHours);

        switch (assessment)
        {
            case WorkflowOperationResult.Success completed:
                Console.WriteLine(
                    $"Assessment completed: " +
                    $"{completed.Assessment.SessionId}, " +
                    $"Risk = {completed.Assessment.RiskScore}");
                break;

            case WorkflowOperationResult.Rejected rejected:
                Console.WriteLine(
                    $"Assessment rejected: {rejected.Reason}");
                break;

            default:
                throw new InvalidOperationException(
                    "Unknown workflow operation result.");
        }

        //a second assessment must be rejected.
        var repeatedAssessment = workflow.Assess(
            session => 10 * session.StressLevel);

        switch (repeatedAssessment)
        {
            case WorkflowOperationResult.Success repeated:
                Console.WriteLine(
                    $"Unexpected repeated success: " +
                    $"{repeated.Assessment.RiskScore}");
                break;

            case WorkflowOperationResult.Rejected rejected:
                Console.WriteLine(
                    $"Repeated assessment rejected: {rejected.Reason}");
                break;

            default:
                throw new InvalidOperationException(
                    "Unknown workflow operation result.");
        }

        break;

    case SessionValidationResult.Invalid invalid:
        Console.WriteLine(
            $"Validation failed: {string.Join(" ", invalid.Errors)}");
        break;

    default:
        throw new InvalidOperationException(
            "Unknown validation result.");
}

