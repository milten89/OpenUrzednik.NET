using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class DefaultNbpExchangeRateTableClientTest
{
    [Fact]
    public void Ctor_2Args_NullHttpClient_ThrowArgumentNullException()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new DefaultNbpExchangeRateTableClient(null!, urlBuilderFactory))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public void Ctor_2Args_NullUrlBuilderFactory_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new DefaultNbpExchangeRateTableClient(httpClient, null!))
            .ParamName.ShouldBe("urlBuilderFactory");
    }

    [Fact]
    public void Ctor_3Args_NullHttpClient_ThrowArgumentNullException()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        var timeProvider = new FakeTimeProvider();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new DefaultNbpExchangeRateTableClient(null!, urlBuilderFactory, timeProvider))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public void Ctor_3Args_NullUrlBuilderFactory_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var timeProvider = new FakeTimeProvider();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new DefaultNbpExchangeRateTableClient(httpClient, null!, timeProvider))
            .ParamName.ShouldBe("urlBuilderFactory");
    }

    [Fact]
    public void Ctor_3Args_NullTimeProvider_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new DefaultNbpExchangeRateTableClient(httpClient, urlBuilderFactory, null!))
            .ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Ctor_2Args_UsesSystemTimeProvider()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        using var httpClient = new HttpClient();

        // Act
        var sut = new DefaultNbpExchangeRateTableClient(httpClient, urlBuilderFactory);

        // Assert
        sut.GetPrivateField<TimeProvider>("_timeProvider").ShouldBeSameAs(TimeProvider.System);
    }
}
