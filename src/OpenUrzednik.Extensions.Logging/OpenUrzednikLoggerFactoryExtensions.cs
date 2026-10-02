using Microsoft.Extensions.Logging;

using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Extensions.Logging;

/// <summary>
/// Creates <see cref="IOpenUrzednikLogger"/>s from an <see cref="ILoggerFactory"/>.
/// </summary>
public static class OpenUrzednikLoggerFactoryExtensions
{
    /// <summary>
    /// Creates a logger for <typeparamref name="TClient"/>, with the client's full type name as the category,
    /// as <see cref="ILogger{TCategoryName}"/> does (e.g. <c>OpenUrzednik.Nbp.Gold.NbpGoldPriceClient</c>).
    /// Filter a whole provider with its namespace (e.g. <c>"OpenUrzednik.Nbp"</c>).
    /// </summary>
    /// <typeparam name="TClient">The client the logger is for.</typeparam>
    /// <param name="loggerFactory">The factory that creates the <see cref="ILogger"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="loggerFactory"/> is null.</exception>
    public static IOpenUrzednikLogger CreateOpenUrzednikLogger<TClient>(this ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        return new OpenUrzednikLogger(loggerFactory.CreateLogger<TClient>());
    }
}
