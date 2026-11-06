using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public partial class HttpErrorMetadataTest
{
    [Theory]
    [MemberData(nameof(ErrorsThatNeverCarryAStatus))]
    public void GetHttpStatusCode_ErrorWithoutHttpResponse_ReturnsNull(OpenUrzednikError error)
    {
        // Act & Assert
        error.GetHttpStatusCode().ShouldBeNull();
    }

    [Fact]
    public void GetHttpStatusCode_NullError_ThrowsArgumentNullException()
    {
        // Arrange
        OpenUrzednikError error = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => error.GetHttpStatusCode()).ParamName.ShouldBe("error");
    }
}
