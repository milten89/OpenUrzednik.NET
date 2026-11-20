using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

public sealed partial class SubjectTest
{
    [Fact]
    public void GetHashCode_SameValuesInDifferentLists_ReturnsSameHashCode()
    {
        // Arrange
        var sut = Create();
        var other = Create();

        // Act
        var result = sut.GetHashCode();

        // Assert
        result.ShouldBe(other.GetHashCode());
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("Nip")]
    [InlineData("Regon")]
    [InlineData("AccountNumbers")]
    public void GetHashCode_HashedMemberDiffers_ReturnsAnotherHashCode(string member)
    {
        // Arrange
        var sut = Create();
        var other = member == "AccountNumbers" ? sut with { AccountNumbers = ["59101010100075912222000000"] } : Change(Create(), member);

        // Act
        var result = sut.GetHashCode();

        // Assert
        result.ShouldNotBe(other.GetHashCode());
    }
}
