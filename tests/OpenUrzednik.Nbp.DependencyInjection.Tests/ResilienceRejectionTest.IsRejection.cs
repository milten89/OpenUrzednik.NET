using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Nbp.DependencyInjection.Tests;

public sealed partial class ResilienceRejectionTest
{
    [Fact]
    public void IsRejection_CreatedRejection_ReturnsTrue()
    {
        // Arrange
        var exception = ResilienceRejection.Create("The circuit is now open.", new InvalidOperationException());

        // Act
        var isRejection = ResilienceRejection.IsRejection(exception);

        // Assert
        isRejection.ShouldBeTrue();
    }

    [Fact]
    public void IsRejection_RejectionWrappedOnce_ReturnsTrue()
    {
        // Arrange
        // .NET Framework's HttpClient may wrap what a handler throws.
        var exception = new HttpRequestException("An error occurred while sending the request.",
            ResilienceRejection.Create("The circuit is now open.", new InvalidOperationException()));

        // Act
        var isRejection = ResilienceRejection.IsRejection(exception);

        // Assert
        isRejection.ShouldBeTrue();
    }

    [Fact]
    public void IsRejection_RejectionWrappedTwice_ReturnsFalse()
    {
        // Arrange
        var exception = new HttpRequestException("Outer.", new HttpRequestException("Inner.",
            ResilienceRejection.Create("The circuit is now open.", new InvalidOperationException())));

        // Act
        var isRejection = ResilienceRejection.IsRejection(exception);

        // Assert
        isRejection.ShouldBeFalse();
    }

    public static TheoryData<Exception> UnmarkedExceptions => new()
    {
        new HttpRequestException("No such host is known."),
        new HttpRequestException("An error occurred while sending the request.", new IOException("The connection was reset.")),
    };

    [Theory]
    [MemberData(nameof(UnmarkedExceptions))]
    public void IsRejection_UnmarkedException_ReturnsFalse(Exception exception)
    {
        // Act
        var isRejection = ResilienceRejection.IsRejection(exception);

        // Assert
        isRejection.ShouldBeFalse();
    }
}
