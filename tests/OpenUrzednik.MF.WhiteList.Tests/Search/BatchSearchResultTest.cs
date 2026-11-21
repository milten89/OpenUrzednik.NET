using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

public sealed class BatchSearchResultTest
{
    private static readonly DateTimeOffset Answered = new(2026, 11, 21, 8, 48, 50, TimeSpan.FromHours(1));

    // Each call builds new lists and a new error, so equality can't come from a reference.
    private static BatchSearchResult Create()
        => new(
        [
            new BatchSearchEntry("3245174504", [new Subject("Nazwa Firmy 1")], null),
            new BatchSearchEntry("1234567890", [], new WhiteListQueryRejectedError("MF White List API rejected the identifier: WL-115 Nieprawidłowy NIP.", "WL-115")),
        ], "r5wrK-99b0ck2", Answered);

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
        // Act
        var result = Create().Equals(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentRequestId_ReturnsFalse()
    {
        // Act
        var result = Create().Equals(Create() with { RequestId = "K4oSY-99b0ck2" });

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
    public void Equals_DifferentEntry_ReturnsFalse()
    {
        // Act
        var result = Create().Equals(Create() with { Entries = [Create().Entries[0], new BatchSearchEntry("1234567890", [new Subject("A")], null)] });

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
        result.ShouldNotBe((Create() with { RequestId = "K4oSY-99b0ck2" }).GetHashCode());
        result.ShouldNotBe((Create() with { RequestDateTime = Answered.AddSeconds(1) }).GetHashCode());
        result.ShouldNotBe((Create() with { Entries = [Create().Entries[0]] }).GetHashCode());
    }
}
