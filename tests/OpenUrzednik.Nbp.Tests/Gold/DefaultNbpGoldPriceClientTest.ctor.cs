using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.UrlBuilder;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class DefaultNbpGoldPriceClientTest
{
    [Fact]
    public void Ctor_NullHttpClient_ThrowArgumentNullException()
    {
        // Arrange
        var urlBuilder = Substitute.For<INbpUrlBuilderFactory>();
        var timeProvider = new FakeTimeProvider();
        
        // Act
        var exception = Record.Exception(() => new DefaultNbpGoldPriceClient(null!, urlBuilder, timeProvider));
        
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
        var exception = Record.Exception(() => new DefaultNbpGoldPriceClient(httpClient, null!, timeProvider));
        
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
        var exception = Record.Exception(() => new DefaultNbpGoldPriceClient(httpClient, urlBuilder, null!));
        
        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("timeProvider");;
    }
}