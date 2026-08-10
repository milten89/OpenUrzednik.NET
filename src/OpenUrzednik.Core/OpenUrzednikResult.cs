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
    {
        ArgumentNullException.ThrowIfNull(errors, nameof(errors));
        var errorsArray = errors.ToArray();
        if (errorsArray.Length == 0)
            throw new InvalidOperationException("Cannot create a failure result without any errors.");

        _errors = errorsArray;
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
public readonly struct OpenUrzednikResult<TValue>
{
    private readonly TValue? _value;
    private readonly OpenUrzednikError[]? _errors;

    /// <summary>
    /// Gets the list of errors associated with the result. If the result is successful, this will be an empty list.
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
        => (_value, _errors) = (value, null);

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
    {
        ArgumentNullException.ThrowIfNull(errors, nameof(errors));

        if (!errors.Any())
            throw new InvalidOperationException("Cannot create a failure result without any errors.");

        _value = default;
        _errors = [.. errors];
    }

    /// <summary>
    /// Converts a failed <see cref="OpenUrzednikResult"/> instance to a failed <see cref="OpenUrzednikResult{TValue}"/> instance. If the result is successful, an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <exception cref="InvalidOperationException">Thrown when the source result is successful.</exception>
    /// <returns>A failed <see cref="OpenUrzednikResult{TValue}"/> instance.</returns>
    public static implicit operator OpenUrzednikResult<TValue>(OpenUrzednikResult result)
        => result.IsFailure ? new(result.Errors) : throw new InvalidOperationException("Cannot convert a successful Result to Result<TValue>.");
}
