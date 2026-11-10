using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

public sealed partial class WhiteListRequestPipelineTest
{
    [Fact]
    public void Ctor_NullConnection_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new WhiteListRequestPipeline(null!, new OpenUrzednikTelemetry()))
            .ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Ctor_NullTelemetry_ThrowsArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var telemetry = new OpenUrzednikTelemetry();
        var connection = WhiteListConnection.CreateSearch(httpClient, null, telemetry, new FakeTimeProvider());

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new WhiteListRequestPipeline(connection, null!))
            .ParamName.ShouldBe("telemetry");
    }
}
