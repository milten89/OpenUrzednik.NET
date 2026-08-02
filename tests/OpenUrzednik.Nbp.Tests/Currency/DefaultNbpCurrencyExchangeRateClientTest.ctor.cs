using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.UrlBuilder;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class DefaultNbpCurrencyExchangeRateClientTest
{
    [Fact]
    public void Ctor_NullHttpClient_ThrowArgumentNullException()
    {
        // Arrange
        var urlBuilder = Substitute.For<INbpUrlBuilderFactory>();
        var timeProvider = new FakeTimeProvider();
        
        // Act
        var exception = Record.Exception(() => new DefaultNbpCurrencyExchangeRateClient(null!, urlBuilder, timeProvider));
        
        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("httpClient");;
    }
    
    [Fact]
    public void Ctor_NullUrlBuilderFactory_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var timeProvider = new FakeTimeProvider();
        
        // Act
        var exception = Record.Exception(() => new DefaultNbpCurrencyExchangeRateClient(httpClient, null!, timeProvider));
        
        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("urlBuilderFactory");;
    }
    
    [Fact]
    public void Ctor_NullTimeProvider_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var urlBuilder = Substitute.For<INbpUrlBuilderFactory>();
        
        // Act
        var exception = Record.Exception(() => new DefaultNbpCurrencyExchangeRateClient(httpClient, urlBuilder, null!));
        
        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("timeProvider");;
    }
}