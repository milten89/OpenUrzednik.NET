using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core;

/// <summary>
/// Works on <see cref="Task"/>s of results: turns failed results into exceptions (<c>EnsureSuccessAsync</c>) and chains operations
/// (<c>MapAsync</c>, <c>BindAsync</c>).
/// </summary>
public static class OpenUrzednikResultExtensions
{
    /// <summary>
    /// Ensures that the <see cref="OpenUrzednikResult"/> is in a successful state. If the result is in a failed state, it throws an exception based on the errors contained in the result.
    /// </summary>
    /// <param name="resultTask">Result task to check.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <exception cref="OpenUrzednikException">Thrown when the result is a failure, as in <see cref="OpenUrzednikResult.EnsureSuccess()"/>.</exception>
    public static async Task EnsureSuccessAsync(this Task<OpenUrzednikResult> resultTask)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        (await resultTask.ConfigureAwait(false)).EnsureSuccess();
    }

    /// <summary>
    /// Ensures that the <see cref="OpenUrzednikResult{T}"/> is in a successful state. If the result is in a failed state, it throws an exception based on the errors contained in the result.
    /// </summary>
    /// <param name="resultTask">Result task to check.</param>
    /// <returns>The value of the result if it is in a successful state.</returns>
    /// <exception cref="OpenUrzednikException">Thrown when the result is a failure, as in <see cref="OpenUrzednikResult.EnsureSuccess()"/>.</exception>
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
}
