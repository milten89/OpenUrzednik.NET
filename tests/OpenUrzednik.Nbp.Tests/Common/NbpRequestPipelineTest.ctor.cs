using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Nbp.Common;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Common;

public sealed partial class NbpRequestPipelineTest
{
    [Fact]
    public void Ctor_NullConnection_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpRequestPipeline(null!, new OpenUrzednikTelemetry()))
            .ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Ctor_NullTelemetry_ThrowsArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var telemetry = new OpenUrzednikTelemetry();
        var connection = NbpConnection.Create(httpClient, null, telemetry, TimeProvider.System);

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpRequestPipeline(connection, null!))
            .ParamName.ShouldBe("telemetry");
    }
}
