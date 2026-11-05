using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;

namespace OpenUrzednik.Nbp.Common;

/// <summary>
/// Converts deserialized NBP payloads to public models. Mappers reject bad API data (e.g. <c>"rates": null</c>)
/// with <see cref="InvalidPayloadException"/>, and <see cref="Map{TDto, TModel}"/> returns it as a <see cref="SerializationError"/>.
/// </summary>
internal static class NbpPayload
{
    internal static void EnsurePresent(object? value, string name)
    {
        if (value is null)
            throw new InvalidPayloadException($"NBP API response is missing '{name}'.");
    }

    /// <summary>
    /// The item with the latest <paramref name="date"/>, or <see langword="null"/> when <paramref name="items"/> is empty.
    /// NBP returns ranges in ascending order, but doesn't document it, so every item is compared; of equal dates the first wins.
    /// </summary>
    internal static T? Latest<T>(T[] items, Func<T, DateOnly> date)
        where T : class
    {
        T? latest = null;
        var latestDay = 0;
        foreach (var item in items)
        {
            EnsurePresent(item, "item");
            var day = date(item).DayNumber;
            if (latest is null || day > latestDay)
            {
                latest = item;
                latestDay = day;
            }
        }

        return latest;
    }

    internal static OpenUrzednikResult<TModel> Map<TDto, TModel>(TDto dto, Func<TDto, TModel> map, OpenUrzednikTelemetry telemetryProvider, IOpenUrzednikSpan traceSpan)
    {
        try
        {
            return OpenUrzednikResult.Success(map(dto));
        }
        catch (InvalidPayloadException ex)
        {
            telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Error, NbpEvents.MappingFailed, ex, "Failed to map NBP response: {reason}", "reason", ex.Message);
            var error = new SerializationError(ex.Message);
            traceSpan.RecordError(error);
            return OpenUrzednikResult.Failure<TModel>(error);
        }
    }
}

/// <summary>
/// Thrown by mappers when a deserialized NBP payload is missing data the model needs.
/// </summary>
internal sealed class InvalidPayloadException(string message) : Exception(message);
