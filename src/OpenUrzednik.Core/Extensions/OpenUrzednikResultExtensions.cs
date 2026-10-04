using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Extensions;

/// <summary>
/// Turns failed results into exceptions (<c>EnsureSuccess</c>, <c>EnsureSuccessAsync</c>), chains operations on <see cref="Task"/>s of results
/// (<c>MapAsync</c>, <c>BindAsync</c>) and combines results (<c>And</c>).
/// </summary>
public static class OpenUrzednikResultExtensions
{
    /// <summary>
    /// Ensures that the <see cref="OpenUrzednikResult"/> is in a successful state. If the result is in a failed state, it throws an exception based on the errors contained in the result.
    /// </summary>
    /// <param name="result">Result to check.</param>
    /// <exception cref="OpenUrzednikException">
    /// Thrown when the result is a failure: the exception of its error, or one <see cref="ValidationException"/> when there are several validation errors.
    /// <see cref="OpenUrzednikException.Errors"/> lists every error of the result.
    /// </exception>
    public static void EnsureSuccess(this OpenUrzednikResult result)
    {
        if (result.IsFailure)
            throw CreateException(result.Errors);
    }

    /// <summary>
    /// Ensures that the <see cref="OpenUrzednikResult"/> is in a successful state. If the result is in a failed state, it throws an exception based on the errors contained in the result.
    /// </summary>
    /// <param name="resultTask">Result task to check.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <exception cref="OpenUrzednikException">Thrown when the result is a failure, as in <see cref="EnsureSuccess(OpenUrzednikResult)"/>.</exception>
    public static async Task EnsureSuccessAsync(this Task<OpenUrzednikResult> resultTask)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        (await resultTask.ConfigureAwait(false)).EnsureSuccess();
    }

    /// <summary>
    /// Ensures that the <see cref="OpenUrzednikResult{T}"/> is in a successful state. If the result is in a failed state, it throws an exception based on the errors contained in the result.
    /// </summary>
    /// <param name="result">Result to check.</param>
    /// <returns>The value of the result if it is in a successful state.</returns>
    /// <exception cref="OpenUrzednikException">Thrown when the result is a failure, as in <see cref="EnsureSuccess(OpenUrzednikResult)"/>.</exception>
    public static T EnsureSuccess<T>(this OpenUrzednikResult<T> result)
    {
        if (result.IsFailure)
            throw CreateException(result.Errors);

        return result.Value;
    }

    /// <summary>
    /// Ensures that the <see cref="OpenUrzednikResult{T}"/> is in a successful state. If the result is in a failed state, it throws an exception based on the errors contained in the result.
    /// </summary>
    /// <param name="resultTask">Result task to check.</param>
    /// <returns>The value of the result if it is in a successful state.</returns>
    /// <exception cref="OpenUrzednikException">Thrown when the result is a failure, as in <see cref="EnsureSuccess(OpenUrzednikResult)"/>.</exception>
    public static async Task<T> EnsureSuccessAsync<T>(this Task<OpenUrzednikResult<T>> resultTask)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        return (await resultTask.ConfigureAwait(false)).EnsureSuccess();
    }

    /// <summary>
    /// Transforms the value of a successful result once the task completes. See <see cref="OpenUrzednikResult{TValue}.Map{TOut}(Func{TValue, TOut})"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value of the source result.</typeparam>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="resultTask">The task returning the source result.</param>
    /// <param name="map">The transformation.</param>
    /// <returns>A successful result with the transformed value, or a failure with the source errors.</returns>
    public static async Task<OpenUrzednikResult<TOut>> MapAsync<T, TOut>(this Task<OpenUrzednikResult<T>> resultTask, Func<T, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(map);

        return (await resultTask.ConfigureAwait(false)).Map(map);
    }

    /// <summary>
    /// Runs <paramref name="next"/> with the value of a successful result once the task completes. See <see cref="OpenUrzednikResult{TValue}.Bind{TOut}(Func{TValue, OpenUrzednikResult{TOut}})"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value of the source result.</typeparam>
    /// <typeparam name="TOut">The type of the value returned by <paramref name="next"/>.</typeparam>
    /// <param name="resultTask">The task returning the source result.</param>
    /// <param name="next">The operation to run with the value.</param>
    /// <returns>The result of <paramref name="next"/>, or a failure with the source errors.</returns>
    public static async Task<OpenUrzednikResult<TOut>> BindAsync<T, TOut>(this Task<OpenUrzednikResult<T>> resultTask, Func<T, OpenUrzednikResult<TOut>> next)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(next);

        return (await resultTask.ConfigureAwait(false)).Bind(next);
    }

    /// <summary>
    /// Runs <paramref name="next"/> with the value of a successful result once the task completes. See <see cref="OpenUrzednikResult{TValue}.BindAsync{TOut}(Func{TValue, Task{OpenUrzednikResult{TOut}}})"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value of the source result.</typeparam>
    /// <typeparam name="TOut">The type of the value returned by <paramref name="next"/>.</typeparam>
    /// <param name="resultTask">The task returning the source result.</param>
    /// <param name="next">The asynchronous operation to run with the value.</param>
    /// <returns>The result of <paramref name="next"/>, or a failure with the source errors.</returns>
    public static async Task<OpenUrzednikResult<TOut>> BindAsync<T, TOut>(this Task<OpenUrzednikResult<T>> resultTask, Func<T, Task<OpenUrzednikResult<TOut>>> next)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(next);

        return await (await resultTask.ConfigureAwait(false)).BindAsync(next).ConfigureAwait(false);
    }

    /// <summary>
    /// Combines two results: successful if both are, otherwise a failure with the errors of both, first's errors first.
    /// </summary>
    /// <param name="first">First result.</param>
    /// <param name="second">Second result.</param>
    /// <returns>The combined result.</returns>
    public static OpenUrzednikResult And(this OpenUrzednikResult first, OpenUrzednikResult second)
    {
        return first.IsSuccess && second.IsSuccess
            ? OpenUrzednikResult.Success()
            : OpenUrzednikResult.Failure(first.Errors.Concat(second.Errors));
    }

    // ADR-0002: one error -> its exception; several validation errors -> one ValidationException listing them;
    // several errors of other kinds -> the first error's exception. Errors always holds every error.
    private static OpenUrzednikException CreateException(IReadOnlyList<OpenUrzednikError> errors)
    {
        if (errors.Count == 1)
            return errors[0].ToException();

        if (errors.All(e => e is ValidationError))
            return new ValidationException([.. errors.Cast<ValidationError>()]);

        var exception = errors[0].ToException();
        exception.SetErrors(errors[0], errors);
        return exception;
    }
}
