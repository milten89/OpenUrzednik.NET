using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Telemetry;

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

    internal static OpenUrzednikResult<TModel> Map<TDto, TModel>(TDto dto, Func<TDto, TModel> map, NbpTelemetryProvider telemetryProvider, IOpenUrzednikSpan traceSpan)
    {
        try
        {
            return OpenUrzednikResult.Success(map(dto));
        }
        catch (InvalidPayloadException ex)
        {
            telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Error, ex, "Failed to map NBP response: {reason}", "reason", ex.Message);
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
