using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

// The providers' copy of src/Http/Infrastructure/ResilienceRejection.cs (reached through OpenUrzednik.Nbp). The DI packages compile
// their own copy, tested in their test projects; both must write the same marker.
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
        // The key and value are shared between package versions: keep them within a major.
        exception.Data["OpenUrzednik.ResilienceRejected"].ShouldBe(true);
        ResilienceRejection.IsRejection(exception).ShouldBeTrue();
    }
}
