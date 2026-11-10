using System.Text.Json.Serialization.Metadata;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;

namespace OpenUrzednik.MF.WhiteList.Common;

/// <summary>
/// The steps every MF White List client method shares: validate, GET with a display path, map to the public model, and record failures
/// on the span. The caller starts the span (its name and tags differ per method) and disposes it.
/// </summary>
/// <remarks>
/// Every request path carries an identifier (NIP, REGON, account number), so every request goes with a display path, which replaces the
/// URI in <c>url.path</c>, logs, messages and <see cref="ErrorResponseContext.RequestPath"/> (ADR-0019, ADR-0023).
/// </remarks>
internal sealed class WhiteListRequestPipeline
{
    private readonly RestRequestExecutor _connection;
    private readonly OpenUrzednikTelemetry _telemetry;

    internal WhiteListRequestPipeline(RestRequestExecutor connection, OpenUrzednikTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(telemetry);

        _connection = connection;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Validates, requests <paramref name="path"/> and maps the whole payload.
    /// </summary>
    /// <param name="traceSpan">The span of the client method; failures are recorded on it.</param>
    /// <param name="operation">The client method name, for logs.</param>
    /// <param name="validation">The combined validation result; on failure no request is sent.</param>
    /// <param name="path">Builds the request path. Called only after validation succeeds, because it takes the validated identifiers.</param>
    /// <param name="displayPath">The route template written instead of the path (e.g. <c>search/nip/{nip}</c>), relative like the path.</param>
    /// <param name="typeInfo">Source-generated metadata of the payload.</param>
    /// <param name="map">Maps the payload to the public model; it throws <see cref="InvalidPayloadException"/> for data the model can't take.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    internal async Task<OpenUrzednikResult<TModel>> GetAsync<TDto, TModel>(IOpenUrzednikSpan traceSpan, string operation, OpenUrzednikResult validation,
        Func<string> path, string displayPath, JsonTypeInfo<TDto> typeInfo, Func<TDto, TModel> map, CancellationToken cancellationToken)
    {
        if (validation.IsFailure)
        {
            if (_telemetry.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetry.Logger.Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.ValidationFailed, null, "Validation failed for {operation}", "operation", operation);
            traceSpan.RecordErrors(validation.Errors);
            return validation;
        }

        var response = await _connection.GetAsync(path(), displayPath, typeInfo, cancellationToken).ConfigureAwait(false);
        if (response.IsFailure)
            traceSpan.RecordErrors(response.Errors);

        return response.Bind(dto => Map(dto, map, traceSpan));
    }

    private OpenUrzednikResult<TModel> Map<TDto, TModel>(TDto dto, Func<TDto, TModel> map, IOpenUrzednikSpan traceSpan)
    {
        try
        {
            return OpenUrzednikResult.Success(map(dto));
        }
        catch (InvalidPayloadException ex)
        {
            _telemetry.Logger.Log(OpenUrzednikLogLevel.Error, WhiteListEvents.MappingFailed, ex, "Failed to map MF White List response: {reason}", "reason", ex.Message);
            var error = new SerializationError(ex.Message);
            traceSpan.RecordError(error);
            return OpenUrzednikResult.Failure<TModel>(error);
        }
    }
}

/// <summary>
/// Thrown by mappers when a deserialized MF White List payload is missing data the model needs. Its message names the field, never a value.
/// </summary>
internal sealed class InvalidPayloadException(string message) : Exception(message);
