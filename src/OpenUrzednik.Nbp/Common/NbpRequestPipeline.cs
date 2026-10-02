using System.Text.Json.Serialization.Metadata;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;

namespace OpenUrzednik.Nbp.Common;

/// <summary>
/// The steps every NBP client method shares: validate, GET, map to the public model, and record failures on the span.
/// The caller starts the span (its name and tags differ per method) and disposes it.
/// </summary>
internal sealed class NbpRequestPipeline
{
    private readonly RestRequestExecutor _connection;
    private readonly OpenUrzednikTelemetry _telemetry;

    internal NbpRequestPipeline(RestRequestExecutor connection, OpenUrzednikTelemetry telemetry)
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
    /// <param name="path">Builds the request path. Called only after validation succeeds, because builders reject invalid input.</param>
    /// <param name="typeInfo">Source-generated metadata of the payload.</param>
    /// <param name="map">Maps the payload to the public model.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    internal async Task<OpenUrzednikResult<TModel>> GetAsync<TDto, TModel>(IOpenUrzednikSpan traceSpan, string operation, OpenUrzednikResult validation,
        Func<string> path, JsonTypeInfo<TDto> typeInfo, Func<TDto, TModel> map, CancellationToken cancellationToken)
    {
        var (response, _) = await RequestAsync(traceSpan, operation, validation, path, typeInfo, cancellationToken).ConfigureAwait(false);

        return response.Bind(dto => NbpPayload.Map(dto, map, _telemetry, traceSpan));
    }

    /// <summary>
    /// Like <see cref="GetAsync{TDto, TModel}"/>, for endpoints that return an array with one item (e.g. one table or one price).
    /// An empty array means NBP has no data for the request, so it becomes a <see cref="NotFoundError"/>. It has no status code,
    /// because the response was <c>200 OK</c>.
    /// </summary>
    internal async Task<OpenUrzednikResult<TModel>> GetFirstAsync<TDto, TModel>(IOpenUrzednikSpan traceSpan, string operation, OpenUrzednikResult validation,
        Func<string> path, JsonTypeInfo<TDto[]> typeInfo, Func<TDto, TModel> map, CancellationToken cancellationToken)
    {
        var (response, requestPath) = await RequestAsync(traceSpan, operation, validation, path, typeInfo, cancellationToken).ConfigureAwait(false);

        return response.Bind(dtos => dtos.Length != 0
            ? NbpPayload.Map(dtos[0], map, _telemetry, traceSpan)
            : EmptyArray<TModel>(traceSpan, requestPath!));
    }

    private async Task<(OpenUrzednikResult<TDto> Response, string? Path)> RequestAsync<TDto>(IOpenUrzednikSpan traceSpan, string operation,
        OpenUrzednikResult validation, Func<string> path, JsonTypeInfo<TDto> typeInfo, CancellationToken cancellationToken)
    {
        if (validation.IsFailure)
        {
            if (_telemetry.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetry.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for {operation}", "operation", operation);
            traceSpan.RecordErrors(validation.Errors);
            return (validation, null);
        }

        var requestPath = path();
        var response = await _connection.GetAsync(requestPath, typeInfo, cancellationToken).ConfigureAwait(false);
        if (response.IsFailure)
            traceSpan.RecordErrors(response.Errors);

        return (response, requestPath);
    }

    private OpenUrzednikResult<TModel> EmptyArray<TModel>(IOpenUrzednikSpan traceSpan, string path)
    {
        if (_telemetry.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
            _telemetry.Logger.Log(OpenUrzednikLogLevel.Debug, null, "NBP API returned empty array for {path}", "path", path);
        var error = new NotFoundError("NBP API returned empty array.");
        traceSpan.RecordError(error);
        return OpenUrzednikResult.Failure<TModel>(error);
    }
}
