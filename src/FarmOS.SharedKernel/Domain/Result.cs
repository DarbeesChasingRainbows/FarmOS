namespace FarmOS.SharedKernel;

/// <summary>
/// A discriminated union representing success or failure.
/// Commands return Result&lt;Guid, DomainError&gt; — never domain data.
/// </summary>
public readonly struct Result<T, E>
{
    private readonly T? _value;
    private readonly E? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    public E Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    private Result(T value)
    {
        _value = value;
        _error = default;
        IsSuccess = true;
    }

    private Result(E error)
    {
        _error = error;
        _value = default;
        IsSuccess = false;
    }

    public static Result<T, E> Success(T value) => new(value);
    public static Result<T, E> Failure(E error) => new(error);

    public static implicit operator Result<T, E>(T value) => Success(value);
    public static implicit operator Result<T, E>(E error) => Failure(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<E, TResult> onFailure)
        => IsSuccess ? onSuccess(_value!) : onFailure(_error!);
}

/// <summary>
/// Standard domain error with a machine-readable code and human-readable message.
/// </summary>
public record DomainError(string Code, string Message)
{
    public static DomainError Validation(string message) => new("VALIDATION_ERROR", message);
    public static DomainError NotFound(string entity, string id) => new("NOT_FOUND", $"{entity} '{id}' not found.");
    public static DomainError Conflict(string message) => new("CONFLICT", message);
    public static DomainError BusinessRule(string message) => new("BUSINESS_RULE", message);
}
