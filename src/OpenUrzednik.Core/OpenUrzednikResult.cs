using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core;

/// <summary>
/// Represents the result of an operation, which can either be successful or failed with associated errors.
/// </summary>
public readonly struct OpenUrzednikResult
{
    private readonly OpenUrzednikError[]? _errors;

    /// <summary>
    /// Gets the list of errors associated with the result. If the result is successful, this will be an empty list.
    /// The list is shared with results created from this one (e.g. by <see cref="Bind{TOut}(Func{OpenUrzednikResult{TOut}})"/>); don't cast it to modify it.
    /// </summary>
    public IReadOnlyList<OpenUrzednikError> Errors => _errors ?? [];

    /// <summary>
    /// Gets a value indicating whether the result is successful. A result is considered successful if it has no associated errors.
    /// </summary>
    public bool IsSuccess => _errors is null;

    /// <summary>
    /// Gets a value indicating whether the result is a failure. A result is considered a failure if it has one or more associated errors.
    /// </summary>
    public bool IsFailure => _errors is not null;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenUrzednikResult"/> struct in a successful state.
    /// </summary>
    public OpenUrzednikResult()
        => _errors = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenUrzednikResult"/> struct in a failed state with a single error.
    /// </summary>
    /// <param name="error">The error associated with the failed result.</param>
    /// <exception cref="ArgumentNullException">Thrown when the error is null.</exception>
    public OpenUrzednikResult(OpenUrzednikError error)
    {
        ArgumentNullException.ThrowIfNull(error, nameof(error));

        _errors = [error];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenUrzednikResult"/> struct in a failed state with multiple errors.
    /// </summary>
    /// <param name="errors">The errors associated with the failed result.</param>
    /// <exception cref="ArgumentNullException">Thrown when the errors collection is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the errors collection is empty.</exception>
    public OpenUrzednikResult(IEnumerable<OpenUrzednikError> errors)
        : this(ToErrorArray(errors)) { }

    // Takes ownership of a non-empty array without copying it; used to forward the errors of another result.
    private OpenUrzednikResult(OpenUrzednikError[] errors)
        => _errors = errors;

    internal static OpenUrzednikError[] ToErrorArray(IEnumerable<OpenUrzednikError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors, nameof(errors));
        var errorsArray = errors.ToArray();
        if (errorsArray.Length == 0)
            throw new InvalidOperationException("Cannot create a failure result without any errors.");

        return errorsArray;
    }

    internal OpenUrzednikError[]? ErrorArray => _errors;

    /// <summary>
    /// Creates a failed result from a single error, so an error can be returned where an <see cref="OpenUrzednikResult"/> is expected.
    /// </summary>
    /// <param name="error">The error associated with the failed result.</param>
    /// <exception cref="ArgumentNullException">Thrown when the error is null.</exception>
    public static implicit operator OpenUrzednikResult(OpenUrzednikError error) => new(error);

    /// <summary>
    /// Runs <paramref name="next"/> when this result is successful; otherwise returns this result's errors without running it.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by <paramref name="next"/>.</typeparam>
    /// <param name="next">The operation to run after a success.</param>
    /// <returns>The result of <paramref name="next"/>, or a failure with this result's errors.</returns>
    public OpenUrzednikResult<TOut> Bind<TOut>(Func<OpenUrzednikResult<TOut>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return _errors is null ? next() : OpenUrzednikResult<TOut>.FromErrors(_errors);
    }

    /// <summary>
    /// Runs <paramref name="next"/> when this result is successful; otherwise returns this result's errors without running it.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by <paramref name="next"/>.</typeparam>
    /// <param name="next">The asynchronous operation to run after a success.</param>
    /// <returns>The result of <paramref name="next"/>, or a failure with this result's errors.</returns>
    public Task<OpenUrzednikResult<TOut>> BindAsync<TOut>(Func<Task<OpenUrzednikResult<TOut>>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return _errors is null ? next() : Task.FromResult(OpenUrzednikResult<TOut>.FromErrors(_errors));
    }

    /// <summary>
    /// Returns the value of <paramref name="onSuccess"/> or <paramref name="onFailure"/>, depending on the state of the result.
    /// </summary>
    /// <typeparam name="TOut">The type of the returned value.</typeparam>
    /// <param name="onSuccess">Called when the result is successful.</param>
    /// <param name="onFailure">Called with the errors when the result is a failure.</param>
    /// <returns>The value returned by the called function.</returns>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<IReadOnlyList<OpenUrzednikError>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return _errors is null ? onSuccess() : onFailure(_errors);
    }

    /// <summary>
    /// Creates a successful <see cref="OpenUrzednikResult"/> instance.
    /// </summary>
    /// <returns>A successful <see cref="OpenUrzednikResult"/> instance.</returns>
    public static OpenUrzednikResult Success() => new();
    /// <summary>
    /// Creates a failed <see cref="OpenUrzednikResult"/> instance with a single error.
    /// </summary>
    /// <param name="error">The error associated with the failed result.</param>
    /// <returns>A failed <see cref="OpenUrzednikResult"/> instance.</returns>
    public static OpenUrzednikResult Failure(OpenUrzednikError error) => new(error);
    /// <summary>
    /// Creates a failed <see cref="OpenUrzednikResult"/> instance with multiple errors.
    /// </summary>
    /// <param name="errors">The errors associated with the failed result.</param>
    /// <returns>A failed <see cref="OpenUrzednikResult"/> instance.</returns>
    public static OpenUrzednikResult Failure(IEnumerable<OpenUrzednikError> errors) => new(errors);

    /// <summary>
    /// Creates a successful <see cref="OpenUrzednikResult{T}"/> instance with the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the value associated with the successful result.</typeparam>
    /// <param name="value">The value associated with the successful result.</param>
    /// <returns>A successful <see cref="OpenUrzednikResult{T}"/> instance.</returns>
    public static OpenUrzednikResult<T> Success<T>(T value) => new(value);
    /// <summary>
    /// Creates a failed <see cref="OpenUrzednikResult{T}"/> instance with a single error.
    /// </summary>
    /// <typeparam name="T">The type of the value associated with the result.</typeparam>
    /// <param name="error">The error associated with the failed result.</param>
    /// <returns>A failed <see cref="OpenUrzednikResult{T}"/> instance.</returns>
    public static OpenUrzednikResult<T> Failure<T>(OpenUrzednikError error) => new(error);
    /// <summary>
    /// Creates a failed <see cref="OpenUrzednikResult{T}"/> instance with multiple errors.
    /// </summary>
    /// <typeparam name="T">The type of the value associated with the result.</typeparam>
    /// <param name="errors">The errors associated with the failed result.</param>
    /// <returns>A failed <see cref="OpenUrzednikResult{T}"/> instance.</returns>
    public static OpenUrzednikResult<T> Failure<T>(IEnumerable<OpenUrzednikError> errors) => new(errors);
}

/// <summary>
/// Represents the result of an operation that can either be successful with a value of type <typeparamref name="TValue"/> or failed with associated errors.
/// </summary>
/// <typeparam name="TValue">The type of the value contained in the result when it succeeds.</typeparam>
/// <remarks>
/// When <typeparamref name="TValue"/> is an error type (e.g. <see cref="OpenUrzednikError"/>), the constructors are ambiguous to read:
/// use <see cref="OpenUrzednikResult.Success{T}(T)"/> and <see cref="OpenUrzednikResult.Failure{T}(OpenUrzednikError)"/> instead.
/// </remarks>
public readonly struct OpenUrzednikResult<TValue>
{
    // The state lives in _errors so the struct stays two fields wide (16 bytes for a reference-type TValue):
    //   success -> the shared empty array, failure -> a non-empty array, default -> null (an uninitialized failure).
    // Read-only, because it is shared by every default instance of this closed type.
    private static readonly IReadOnlyList<OpenUrzednikError> UninitializedErrors = Array.AsReadOnly<OpenUrzednikError>(
        [new UnknownError($"The result was not initialized: default({nameof(OpenUrzednikResult)}<T>) was used instead of a result created with Success or Failure.")]);

    private readonly TValue? _value;
    private readonly OpenUrzednikError[]? _errors;

    /// <summary>
    /// Gets the list of errors associated with the result. If the result is successful, this will be an empty list.
    /// A <see langword="default"/> instance is a failure with a single <see cref="UnknownError"/>.
    /// The list is shared with results created from this one (e.g. by <see cref="Map{TOut}(Func{TValue, TOut})"/>); don't cast it to modify it.
    /// </summary>
    public IReadOnlyList<OpenUrzednikError> Errors => (IReadOnlyList<OpenUrzednikError>?)_errors ?? UninitializedErrors;

    /// <summary>
    /// Gets a value indicating whether the result is successful. Only results created with a value are successful;
    /// a <see langword="default"/> instance is not.
    /// </summary>
    public bool IsSuccess => _errors is { Length: 0 };

    /// <summary>
    /// Gets a value indicating whether the result is a failure. A result is a failure if it was created with errors or is a <see langword="default"/> instance.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value associated with the result. If the result is in a failed state, accessing this property will throw an <see cref="InvalidOperationException"/>.
    /// </summary>
    public TValue Value
        => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of a failed result.");

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> to prevent creating a generic result without explicitly providing a value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the constructor is invoked directly.</exception>
    public OpenUrzednikResult()
        => throw new InvalidOperationException("Cannot create a generic result without explicitly providing a value.");

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenUrzednikResult{TValue}"/> struct in a successful state with the specified value.
    /// </summary>
    /// <param name="value">The value associated with the result.</param>
    public OpenUrzednikResult(TValue value)
        => (_value, _errors) = (value, Array.Empty<OpenUrzednikError>());

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenUrzednikResult{TValue}"/> struct in a failed state with a single error.
    /// </summary>
    /// <param name="error">The error associated with the result.</param>
    /// <exception cref="ArgumentNullException">Thrown when the error is null.</exception>
    public OpenUrzednikResult(OpenUrzednikError error)
    {
        ArgumentNullException.ThrowIfNull(error, nameof(error));

        _value = default;
        _errors = [error];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenUrzednikResult{TValue}"/> struct in a failed state with multiple errors.
    /// </summary>
    /// <param name="errors">The errors associated with the result.</param>
    /// <exception cref="ArgumentNullException">Thrown when the errors collection is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the errors collection is empty.</exception>
    public OpenUrzednikResult(IEnumerable<OpenUrzednikError> errors)
        : this(OpenUrzednikResult.ToErrorArray(errors)) { }

    // Takes ownership of a non-empty array without copying it; used to forward the errors of another result.
    // It is private, so callers outside always get the public overloads.
    private OpenUrzednikResult(OpenUrzednikError[] errors)
    {
        Debug.Assert(errors.Length > 0, "A failure needs at least one error.");
        _value = default;
        _errors = errors;
    }

    internal static OpenUrzednikResult<TValue> FromErrors(OpenUrzednikError[] errors) => new(errors);

    // The errors of a failure; a default instance gets its UnknownError.
    private OpenUrzednikError[] FailureErrors => _errors ?? [UninitializedErrors[0]];

    /// <summary>
    /// Gets the value when the result is successful.
    /// </summary>
    /// <param name="value">The value when this method returns <see langword="true"/>; otherwise <see langword="default"/>.</param>
    /// <returns><see langword="true"/> when the result is successful.</returns>
    public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
    {
        value = _value;
        return IsSuccess;
    }

    /// <summary>
    /// Transforms the value of a successful result. A failure is returned with its errors, and <paramref name="map"/> isn't called.
    /// </summary>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="map">The transformation. It must not fail; use <see cref="Bind{TOut}(Func{TValue, OpenUrzednikResult{TOut}})"/> if it can.</param>
    /// <returns>A successful result with the transformed value, or a failure with this result's errors.</returns>
    public OpenUrzednikResult<TOut> Map<TOut>(Func<TValue, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(map);

        return IsSuccess ? new OpenUrzednikResult<TOut>(map(_value!)) : OpenUrzednikResult<TOut>.FromErrors(FailureErrors);
    }

    /// <summary>
    /// Runs <paramref name="next"/> with the value of a successful result. A failure is returned with its errors, and <paramref name="next"/> isn't called.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by <paramref name="next"/>.</typeparam>
    /// <param name="next">The operation to run with the value.</param>
    /// <returns>The result of <paramref name="next"/>, or a failure with this result's errors.</returns>
    public OpenUrzednikResult<TOut> Bind<TOut>(Func<TValue, OpenUrzednikResult<TOut>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return IsSuccess ? next(_value!) : OpenUrzednikResult<TOut>.FromErrors(FailureErrors);
    }

    /// <summary>
    /// Runs <paramref name="next"/> with the value of a successful result. A failure is returned with its errors, and <paramref name="next"/> isn't called.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by <paramref name="next"/>.</typeparam>
    /// <param name="next">The asynchronous operation to run with the value.</param>
    /// <returns>The result of <paramref name="next"/>, or a failure with this result's errors.</returns>
    public Task<OpenUrzednikResult<TOut>> BindAsync<TOut>(Func<TValue, Task<OpenUrzednikResult<TOut>>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return IsSuccess ? next(_value!) : Task.FromResult(OpenUrzednikResult<TOut>.FromErrors(FailureErrors));
    }

    /// <summary>
    /// Returns the value of <paramref name="onSuccess"/> or <paramref name="onFailure"/>, depending on the state of the result.
    /// </summary>
    /// <typeparam name="TOut">The type of the returned value.</typeparam>
    /// <param name="onSuccess">Called with the value when the result is successful.</param>
    /// <param name="onFailure">Called with the errors when the result is a failure.</param>
    /// <returns>The value returned by the called function.</returns>
    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<IReadOnlyList<OpenUrzednikError>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(_value!) : onFailure(Errors);
    }

    /// <summary>
    /// Converts a failed <see cref="OpenUrzednikResult"/> instance to a failed <see cref="OpenUrzednikResult{TValue}"/> instance. If the result is successful, an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <exception cref="InvalidOperationException">Thrown when the source result is successful.</exception>
    /// <returns>A failed <see cref="OpenUrzednikResult{TValue}"/> instance.</returns>
    public static implicit operator OpenUrzednikResult<TValue>(OpenUrzednikResult result)
        => result.ErrorArray is { } errors ? FromErrors(errors) : throw new InvalidOperationException("Cannot convert a successful Result to Result<TValue>.");
}
