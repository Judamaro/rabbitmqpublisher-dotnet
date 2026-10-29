namespace RabbitMQPublisher.Domain.Common;

/// <summary>
/// Represents the outcome of an operation that either succeeds or fails with a <see cref="ResultError"/>.
/// Implements the Result pattern so expected failures are modeled explicitly instead of using exceptions.
/// </summary>
public class Result
{
    /// <summary>Initializes a new instance of the <see cref="Result"/> class.</summary>
    /// <param name="error">The error, or <see langword="null"/> when the operation succeeded.</param>
    protected Result(ResultError? error)
    {
        Error = error;
    }

    /// <summary>Gets the error of a failed operation, or <see langword="null"/> when it succeeded.</summary>
    public ResultError? Error { get; }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    /// <summary>Creates a successful result.</summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new(null);

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(ResultError error) => new(error);

    /// <summary>Converts an error into a failed result.</summary>
    /// <param name="error">The error describing the failure.</param>
    public static implicit operator Result(ResultError error) => Failure(error);
}

/// <summary>
/// Represents the outcome of an operation that returns a <typeparamref name="TValue"/> on success.
/// </summary>
/// <typeparam name="TValue">Type of the value returned on success.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue value) : base(null)
    {
        _value = value;
    }

    private Result(ResultError error) : base(error)
    {
    }

    /// <summary>Gets the value of a successful result.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    /// <summary>Creates a successful result holding <paramref name="value"/>.</summary>
    /// <param name="value">The value of the result.</param>
    /// <returns>A successful result.</returns>
    public static Result<TValue> Success(TValue value) => new(value);

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failed result.</returns>
    public static new Result<TValue> Failure(ResultError error) => new(error);

    /// <summary>Converts a value into a successful result.</summary>
    /// <param name="value">The value of the result.</param>
    public static implicit operator Result<TValue>(TValue value) => Success(value);

    /// <summary>Converts an error into a failed result.</summary>
    /// <param name="error">The error describing the failure.</param>
    public static implicit operator Result<TValue>(ResultError error) => Failure(error);
}
