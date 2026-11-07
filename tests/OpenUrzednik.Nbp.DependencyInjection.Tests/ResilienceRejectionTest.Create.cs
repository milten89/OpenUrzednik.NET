using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Nbp.DependencyInjection.Tests;

// The DI package's copy of src/Http/Infrastructure/ResilienceRejection.cs, the one that marks the rejections.
public sealed partial class ResilienceRejectionTest
{
    [Fact]
    public void Create_Rejection_ReturnsHttpRequestExceptionMarkedInData()
    {
        // Arrange
        var rejection = new InvalidOperationException("The circuit is now open.");

        // Act
        var exception = ResilienceRejection.Create(rejection.Message, rejection);

        // Assert
        exception.Message.ShouldBe(rejection.Message);
        exception.InnerException.ShouldBeSameAs(rejection);
        // The key is shared with the providers' own copy, which may come from another package version: keep it within a major.
        exception.Data["OpenUrzednik.ResilienceRejected"].ShouldBe(true);
    }
}
