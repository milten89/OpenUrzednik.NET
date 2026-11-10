using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

// Shared helpers; tests are in one file per method.
public sealed partial class WhiteListConnectionTest
{
    private static OpenUrzednikTelemetry Telemetry() => new(NullOpenUrzednikLogger.Instance, NullOpenUrzednikTraceSource.Instance);

    private static RestRequestExecutor CreateSearch(HttpClient httpClient, WhiteListOptions? options)
        => WhiteListConnection.CreateSearch(httpClient, options, Telemetry(), new FakeTimeProvider());

    private static RestRequestExecutor CreateStatus(HttpClient httpClient, WhiteListOptions? options)
        => WhiteListConnection.CreateStatus(httpClient, options, Telemetry(), new FakeTimeProvider());
}
