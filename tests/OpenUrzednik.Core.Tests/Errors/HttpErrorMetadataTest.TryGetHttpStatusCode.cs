using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public partial class HttpErrorMetadataTest
{
    [Theory]
    [MemberData(nameof(ErrorsThatNeverCarryAStatus))]
    public void TryGetHttpStatusCode_ErrorWithoutHttpResponse_ReturnsFalse(OpenUrzednikError error)
    {
        // Act
        var found = error.TryGetHttpStatusCode(out var statusCode);

        // Assert
        found.ShouldBeFalse();
        statusCode.ShouldBe(0);
    }

    [Fact]
    public void TryGetHttpStatusCode_NullError_ThrowsArgumentNullException()
    {
        // Arrange
        OpenUrzednikError error = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => error.TryGetHttpStatusCode(out _)).ParamName.ShouldBe("error");
    }
}
