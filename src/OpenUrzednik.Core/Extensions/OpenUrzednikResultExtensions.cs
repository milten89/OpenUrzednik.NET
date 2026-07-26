namespace OpenUrzednik.Core.Extensions;

public static class OpenUrzednikResultExtensions
{
    /// <summary>
    /// Ensures that the <see cref="OpenUrzednikResult"/> is in a successful state. If the result is in a failed state, it throws an exception based on the errors contained in the result.
    /// </summary>
    /// <param name="result">Result to check.</param>
    /// <exception cref="InvalidOperationException">Thrown when the result is in a failed state but contains no errors.</exception>
    /// <exception cref="AggregateException">Thrown when the result is in a failed state and contains multiple errors.</exception>
    public static void EnsureSuccess(this OpenUrzednikResult result)
    {
        if (result.IsFailure)
        {
            if (result.Errors.Count == 0)
                throw new InvalidOperationException("Result is in a failed state but contains no errors.");
            else if (result.Errors.Count == 1)
                throw result.Errors[0].ToException();
            else
                throw new AggregateException(result.Errors.Select(e => e.ToException()));
        }
    }

    /// <summary>
    /// Ensures that the <see cref="OpenUrzednikResult"/> is in a successful state. If the result is in a failed state, it throws an exception based on the errors contained in the result.
    /// </summary>
    /// <param name="resultTask">Result task to check.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
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
    public static T EnsureSuccess<T>(this OpenUrzednikResult<T> result)
    {
        if (result.IsFailure)
        {
            if (result.Errors.Count == 0)
                throw new InvalidOperationException("Result is in a failed state but contains no errors.");
            else if (result.Errors.Count == 1)
                throw result.Errors[0].ToException();
            else
                throw new AggregateException(result.Errors.Select(e => e.ToException()));
        }

        return result.Value;
    }

    /// <summary>
    /// Ensures that the <see cref="OpenUrzednikResult{T}"/> is in a successful state. If the result is in a failed state, it throws an exception based on the errors contained in the result.
    /// </summary>
    /// <param name="resultTask">Result task to check.</param>
    /// <returns>The value of the result if it is in a successful state.</returns>
    public static async Task<T> EnsureSuccessAsync<T>(this Task<OpenUrzednikResult<T>> resultTask)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        return (await resultTask.ConfigureAwait(false)).EnsureSuccess();
    }
}