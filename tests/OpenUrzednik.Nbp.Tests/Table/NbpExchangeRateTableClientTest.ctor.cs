using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Telemetry;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class NbpExchangeRateTableClientTest
{
    [Fact]
    public void Ctor_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpExchangeRateTableClient(null!))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public void CtorWithOptions_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpExchangeRateTableClient(null!, new NbpOptions(), Substitute.For<INbpUrlBuilderFactory>(), new FakeTimeProvider()))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public void Ctor_HttpClientOnly_UsesDefaults()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        var sut = new NbpExchangeRateTableClient(httpClient);

        // Assert
        sut.GetPrivateField<TimeProvider>("_timeProvider").ShouldBeSameAs(TimeProvider.System);
        var telemetryProvider = sut.GetPrivateField<NbpTelemetryProvider>("_telemetryProvider");
        telemetryProvider.Logger.ShouldBeSameAs(NullOpenUrzednikLogger.Instance);
        telemetryProvider.Tracer.ShouldBeSameAs(NullOpenUrzednikTraceSource.Instance);
        sut.GetPrivateField<INbpUrlBuilderFactory>("_urlBuilderFactory").ShouldBeOfType<NbpUrlBuilderFactory>();
    }

    [Fact]
    public void Ctor_CustomComponents_UsesThem()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        var timeProvider = new FakeTimeProvider();
        var logger = Substitute.For<IOpenUrzednikLogger>();
        var traceSource = Substitute.For<IOpenUrzednikTraceSource>();

        // Act
        var sut = new NbpExchangeRateTableClient(httpClient, new NbpOptions(), urlBuilderFactory, timeProvider, logger, traceSource);

        // Assert
        sut.GetPrivateField<TimeProvider>("_timeProvider").ShouldBeSameAs(timeProvider);
        var telemetryProvider = sut.GetPrivateField<NbpTelemetryProvider>("_telemetryProvider");
        telemetryProvider.Logger.ShouldBeSameAs(logger);
        telemetryProvider.Tracer.ShouldBeSameAs(traceSource);
        sut.GetPrivateField<INbpUrlBuilderFactory>("_urlBuilderFactory").ShouldBeSameAs(urlBuilderFactory);
    }

    [Fact]
    public void Ctor_InvalidOptions_ThrowsArgumentException()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentException>(() => new NbpExchangeRateTableClient(httpClient, new NbpOptions { ApiUrl = "http://api.nbp.pl/api/" }))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Ctor_Always_LeavesHttpClientUnchanged()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var timeout = httpClient.Timeout;

        // Act
        _ = new NbpExchangeRateTableClient(httpClient, new NbpOptions { ApiUrl = "https://proxy.example.com/", Timeout = TimeSpan.FromSeconds(5) });

        // Assert
        httpClient.BaseAddress.ShouldBeNull();
        httpClient.Timeout.ShouldBe(timeout);
    }
}
