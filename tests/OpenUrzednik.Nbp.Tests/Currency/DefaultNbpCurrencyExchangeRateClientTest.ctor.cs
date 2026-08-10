using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class DefaultNbpCurrencyExchangeRateClientTest
{
    [Fact]
    public void Ctor_2Args_NullHttpClient_ThrowArgumentNullException()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new DefaultNbpCurrencyExchangeRateClient(null!, urlBuilderFactory))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public void Ctor_2Args_NullUrlBuilderFactory_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new DefaultNbpCurrencyExchangeRateClient(httpClient, null!))
            .ParamName.ShouldBe("urlBuilderFactory");
    }

    [Fact]
    public void Ctor_3Args_NullHttpClient_ThrowArgumentNullException()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        var timeProvider = new FakeTimeProvider();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new DefaultNbpCurrencyExchangeRateClient(null!, urlBuilderFactory, timeProvider))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public void Ctor_3Args_NullUrlBuilderFactory_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var timeProvider = new FakeTimeProvider();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new DefaultNbpCurrencyExchangeRateClient(httpClient, null!, timeProvider))
            .ParamName.ShouldBe("urlBuilderFactory");
    }

    [Fact]
    public void Ctor_3Args_NullTimeProvider_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new DefaultNbpCurrencyExchangeRateClient(httpClient, urlBuilderFactory, null!))
            .ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Ctor_2Args_UsesSystemTimeProvider()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        using var httpClient = new HttpClient();

        // Act
        var sut = new DefaultNbpCurrencyExchangeRateClient(httpClient, urlBuilderFactory);

        // Assert
        sut.GetPrivateField<TimeProvider>("_timeProvider").ShouldBeSameAs(TimeProvider.System);
    }
}
