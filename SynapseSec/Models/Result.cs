namespace SynapseSec.Models;

public enum ErrorCode
{
    BrainwaveCorrupted,   // error 1: wrong metrics like negative time, or stress above 10
    ImplantNotRecognized, // error 2: implant is unknown, or not certified
    MentalBreakdown,      // error 3: risk score is too high, or state is wrong
    OperatorNotFound,     // operator id was not found
    FileNotFound,         // json file does not exist
    MalformedFile,        // json file has wrong content
    NativeCallFailed      // native c library failed, or got bad numbers
}

public sealed record NeuralError(ErrorCode Code, string Message);

// stores lookup result when value is found, or missing
public abstract record LookupResult<T>
{
    private LookupResult() { }
    public sealed record Found(T Value) : LookupResult<T>;
    public sealed record NotFound(string Key) : LookupResult<T>;
}

// result type with map, and bind methods
public abstract record Result<T, E>
{
    private Result() { }
    public sealed record Success(T Value) : Result<T, E>;
    public sealed record Failure(E Error) : Result<T, E>;

    public Result<U, E> Map<U>(Func<T, U> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return this switch
        {
            Success s => new Result<U, E>.Success(mapper(s.Value)),
            Failure f => new Result<U, E>.Failure(f.Error),
            _ => throw new InvalidOperationException("Unknown result state.")
        };
    }

    public Result<U, E> Bind<U>(Func<T, Result<U, E>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return this switch
        {
            Success s => binder(s.Value),
            Failure f => new Result<U, E>.Failure(f.Error),
            _ => throw new InvalidOperationException("Unknown result state.")
        };
    }
}