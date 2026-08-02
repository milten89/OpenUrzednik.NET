using System.Reflection;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class DefaultNbpGoldPriceClientTest
{
    [Fact]
    public void Ctor_2Args_NullHttpClient_ThrowArgumentNullException()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        
        // Act
        var exception = Record.Exception(() => new DefaultNbpGoldPriceClient(null!, urlBuilderFactory));
        
        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("httpClient");;
    }
    
    [Fact]
    public void Ctor_2Args_NullUrlBuilderFactory_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        
        // Act
        var exception = Record.Exception(() => new DefaultNbpGoldPriceClient(httpClient, null!));
        
        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("urlBuilderFactory");;
    }
    
    [Fact]
    public void Ctor_3Args_NullHttpClient_ThrowArgumentNullException()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        var timeProvider = new FakeTimeProvider();
        
        // Act
        var exception = Record.Exception(() => new DefaultNbpGoldPriceClient(null!, urlBuilderFactory, timeProvider));
        
        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("httpClient");;
    }
    
    [Fact]
    public void Ctor_3Args_NullUrlBuilderFactory_ThrowArgumentNullException()
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
    public void Ctor_3Args_NullTimeProvider_ThrowArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        
        // Act
        var exception = Record.Exception(() => new DefaultNbpGoldPriceClient(httpClient, urlBuilderFactory, null!));
        
        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("timeProvider");;
    }
    
    [Fact]
    public void Ctor_ValidParameters_GetSingleInstanceOfGoldBuilder()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        var timeProvider = new FakeTimeProvider();
        
        // Act
        new DefaultNbpGoldPriceClient(httpClient, urlBuilderFactory, timeProvider);
        
        // Assert
        urlBuilderFactory.Received(1).GetGoldBuilder();
    }
    
    [Fact]
    public void Ctor_2Args_UsesSystemTimeProvider()
    {
        // Arrange
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        using var httpClient = new HttpClient();

        // Act
        var sut = new DefaultNbpGoldPriceClient(httpClient, urlBuilderFactory);

        // Assert
        sut.GetPrivateField<TimeProvider>("_timeProvider").ShouldBeSameAs(TimeProvider.System);
    }
}