using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.Telemetry;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Common;

public partial class NbpConnectionTest
{
    [Fact]
    public void Ctor_NullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange
        var telemetryProvider = new NbpTelemetryProvider(NullOpenUrzednikLogger.Instance, NullOpenUrzednikTraceSource.Instance);

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpConnection(null!, new Uri(NbpOptions.DefaultApiUrl), null, telemetryProvider, TimeProvider.System))
            .ParamName.ShouldBe("httpClient");
    }
}
