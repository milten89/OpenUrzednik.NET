using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Telemetry;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class NbpGoldPriceClientTest
{
    [Fact]
    public void Ctor_4Args_NullHttpClient_ThrowArgumentNullException()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpGoldPriceClient(null!, urlBuilderFactory, null, null))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public void Ctor_4Args_NullUrlBuilderFactory_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpGoldPriceClient(httpClient, null!, null, null))
            .ParamName.ShouldBe("urlBuilderFactory");
    }

    [Fact]
    public void Ctor_5Args_NullHttpClient_ThrowArgumentNullException()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        var timeProvider = new FakeTimeProvider();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpGoldPriceClient(null!, urlBuilderFactory, timeProvider, null, null))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public void Ctor_5Args_NullUrlBuilderFactory_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var timeProvider = new FakeTimeProvider();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpGoldPriceClient(httpClient, null!, timeProvider, null, null))
            .ParamName.ShouldBe("urlBuilderFactory");
    }

    [Fact]
    public void Ctor_5Args_NullTimeProvider_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpGoldPriceClient(httpClient, urlBuilderFactory, null!, null, null))
            .ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Ctor_ValidParameters_GetSingleInstanceOfGoldBuilder()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        var timeProvider = new FakeTimeProvider();

        // Act
        new NbpGoldPriceClient(httpClient, urlBuilderFactory, timeProvider);

        // Assert
        urlBuilderFactory.Received(1).GetGoldBuilder();
    }

    [Fact]
    public void Ctor_4Args_UsesSystemTimeProvider()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        using var httpClient = new HttpClient();

        // Act
        var sut = new NbpGoldPriceClient(httpClient, urlBuilderFactory, null, null);

        // Assert
        sut.GetPrivateField<TimeProvider>("_timeProvider").ShouldBeSameAs(TimeProvider.System);
    }

    [Fact]
    public void Ctor_4Args_UsesNullLoggerAndTraceSource()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        using var httpClient = new HttpClient();

        // Act
        var sut = new NbpGoldPriceClient(httpClient, urlBuilderFactory, null, null);

        // Assert
        var telemetryProvider = sut.GetPrivateField<NbpTelemetryProvider>("_telemetryProvider");
        telemetryProvider.Logger.ShouldBeSameAs(NullOpenUrzednikLogger.Instance);
        telemetryProvider.Tracer.ShouldBeSameAs(NullOpenUrzednikTraceSource.Instance);
    }

    [Fact]
    public void Ctor_5Args_UsesNullLoggerAndTraceSource()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        using var httpClient = new HttpClient();
        var timeProvider = new FakeTimeProvider();

        // Act
        var sut = new NbpGoldPriceClient(httpClient, urlBuilderFactory, timeProvider, null, null);

        // Assert
        var telemetryProvider = sut.GetPrivateField<NbpTelemetryProvider>("_telemetryProvider");
        telemetryProvider.Logger.ShouldBeSameAs(NullOpenUrzednikLogger.Instance);
        telemetryProvider.Tracer.ShouldBeSameAs(NullOpenUrzednikTraceSource.Instance);
    }
}
