using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public partial class RestRequestExecutorTest
{
    private static readonly Uri BaseAddress = new("https://api.example.com/");

    [Fact]
    public void Ctor_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new RestRequestExecutor(null!, BaseAddress, Profile))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public void Ctor_NullBaseAddress_ThrowsArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new RestRequestExecutor(httpClient, null!, Profile))
            .ParamName.ShouldBe("baseAddress");
    }

    [Fact]
    public void Ctor_NullProfile_ThrowsArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new RestRequestExecutor(httpClient, BaseAddress, null!))
            .ParamName.ShouldBe("profile");
    }

    [Fact]
    public void Ctor_RelativeBaseAddress_ThrowsArgumentException()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentException>(() => new RestRequestExecutor(httpClient, new Uri("api/", UriKind.Relative), Profile))
            .ParamName.ShouldBe("baseAddress");
    }

    // The range itself is tested on RestOptions.IsValidTimeout; this checks the constructor's own exception.
    [Fact]
    public void Ctor_InvalidTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act && Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new RestRequestExecutor(httpClient, BaseAddress, Profile, TimeSpan.Zero))
            .ParamName.ShouldBe("timeout");
    }

    [Fact]
    public void Ctor_TimeoutOfIntMaxValueMilliseconds_KeepsIt()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var timeout = TimeSpan.FromTicks(int.MaxValue * TimeSpan.TicksPerMillisecond);

        // Act
        var sut = new RestRequestExecutor(httpClient, BaseAddress, Profile, timeout);

        // Assert
        sut.Timeout.ShouldBe(timeout);
    }

    [Fact]
    public void Ctor_NoTimeout_UsesHttpClientTimeout()
    {
        // Arrange
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(42) };

        // Act
        var sut = new RestRequestExecutor(httpClient, BaseAddress, Profile);

        // Assert
        sut.BaseAddress.ShouldBe(BaseAddress);
        sut.Timeout.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Fact]
    public void Ctor_InfiniteTimeout_HasNoOwnDeadline()
    {
        // Arrange
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(42) };

        // Act
        var sut = new RestRequestExecutor(httpClient, BaseAddress, Profile, Timeout.InfiniteTimeSpan);

        // Assert
        sut.Timeout.ShouldBeNull();
    }
}
