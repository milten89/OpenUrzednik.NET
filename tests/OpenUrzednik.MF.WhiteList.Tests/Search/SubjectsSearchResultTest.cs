using OpenUrzednik.MF.WhiteList.Search;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

public sealed class SubjectsSearchResultTest
{
    private static readonly DateTimeOffset Answered = new(2026, 11, 20, 22, 45, 21, TimeSpan.FromHours(1));

    // Each call builds a new list, so equality can't come from the list reference.
    private static SubjectsSearchResult Create()
        => new([new Subject("A") { Nip = "5260250274" }, new Subject("B")], "R43vM-99alln9", Answered);

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
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // Act
        var result = Create().Equals(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentRequestId_ReturnsFalse()
    {
        // Act
        var result = Create().Equals(Create() with { RequestId = "Meupq-99alll1" });

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentRequestDateTime_ReturnsFalse()
    {
        // Act
        var result = Create().Equals(Create() with { RequestDateTime = Answered.AddSeconds(1) });

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentSubject_ReturnsFalse()
    {
        // Act
        var result = Create().Equals(Create() with { Subjects = [new Subject("A") { Nip = "5260250274" }, new Subject("C")] });

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_SameValuesInDifferentLists_ReturnsSameHashCode()
    {
        // Act
        var result = Create().GetHashCode();

        // Assert
        result.ShouldBe(Create().GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_ReturnsAnotherHashCode()
    {
        // Act
        var result = Create().GetHashCode();

        // Assert
        result.ShouldNotBe((Create() with { RequestId = "Meupq-99alll1" }).GetHashCode());
        result.ShouldNotBe((Create() with { RequestDateTime = Answered.AddSeconds(1) }).GetHashCode());
        result.ShouldNotBe((Create() with { Subjects = [new Subject("A")] }).GetHashCode());
    }
}
