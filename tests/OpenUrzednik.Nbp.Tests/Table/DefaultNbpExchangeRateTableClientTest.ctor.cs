using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class DefaultNbpExchangeRateTableClientTest
{
    [Fact]
    public void Ctor_NullHttpClient_ThrowArgumentNullException()
    {
        // Arrange
        var urlBuilder = Substitute.For<INbpUrlBuilderFactory>();
        var timeProvider = new FakeTimeProvider();
        
        // Act
        var exception = Record.Exception(() => new DefaultNbpExchangeRateTableClient(null!, urlBuilder, timeProvider));
        
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
        var exception = Record.Exception(() => new DefaultNbpExchangeRateTableClient(httpClient, null!, timeProvider));
        
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
        var exception = Record.Exception(() => new DefaultNbpExchangeRateTableClient(httpClient, urlBuilder, null!));
        
        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("timeProvider");;
    }
}