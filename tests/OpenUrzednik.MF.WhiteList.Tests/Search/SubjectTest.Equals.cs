using OpenUrzednik.MF.WhiteList.Search;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

public sealed partial class SubjectTest
{
    [Fact]
    public void Equals_SameValuesInDifferentLists_ReturnsTrue()
    {
        // Arrange
        var sut = Create();
        var other = Create();

        // Act
        var result = sut.Equals(other);

        // Assert
        result.ShouldBeTrue();
        (sut == other).ShouldBeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        var sut = Create();

        // Act
        var result = sut.Equals(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public void Equals_OneMemberDiffers_ReturnsFalse(string member)
    {
        // Arrange
        var sut = Create();
        var other = Change(Create(), member);

        // Act
        var result = sut.Equals(other);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DefaultLists_AreEqual()
    {
        // Arrange
        var sut = new Subject("A");

        // Act
        var result = sut.Equals(new Subject("A"));

        // Assert
        result.ShouldBeTrue();
        sut.Representatives.ShouldBeEmpty();
        sut.AuthorizedClerks.ShouldBeEmpty();
        sut.Partners.ShouldBeEmpty();
        sut.AccountNumbers.ShouldBeEmpty();
    }
}
