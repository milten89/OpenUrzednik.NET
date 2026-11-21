using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.MF.WhiteList.Search;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

public sealed class BatchSearchEntryTest
{
    private const string RequestId = "r5wrK-99b0ck2";
    private static readonly DateTimeOffset Answered = new(2026, 11, 21, 8, 48, 50, TimeSpan.FromHours(1));

    // Each call builds a new list and a new error, so equality can't come from a reference.
    private static BatchSearchEntry Found() => new("3245174504", [new Subject("Nazwa Firmy 1"), new Subject("B")], null);

    private static BatchSearchEntry NotFound(string requestId = RequestId)
        => new("5260250274", [], new WhiteListNotFoundError("Not on the list.", requestId, Answered));

    private static BatchSearchEntry Rejected(string message = "Rejected: WL-115.", string? code = "WL-115")
        => new("1234567890", [], new WhiteListQueryRejectedError(message, code));

    [Fact]
    public void IsSuccess_NoError_ReturnsTrue()
    {
        // Act & Assert
        Found().IsSuccess.ShouldBeTrue();
        NotFound().IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void Equals_SameValuesInDifferentInstances_ReturnsTrue()
    {
        // Act & Assert
        Found().Equals(Found()).ShouldBeTrue();
        NotFound().Equals(NotFound()).ShouldBeTrue();
        Rejected().Equals(Rejected()).ShouldBeTrue();
        (Rejected() == Rejected()).ShouldBeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // Act
        var result = Found().Equals(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentIdentifier_ReturnsFalse()
    {
        // Act
        var result = Found().Equals(Found() with { Identifier = "5260250274" });

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentSubject_ReturnsFalse()
    {
        // Act
        var result = Found().Equals(Found() with { Subjects = [new Subject("Nazwa Firmy 1"), new Subject("C")] });

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ErrorOnOneSideOnly_ReturnsFalse()
    {
        // Arrange
        var withError = Found() with { Error = Rejected().Error };

        // Act & Assert
        Found().Equals(withError).ShouldBeFalse();
        withError.Equals(Found()).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentErrorType_ReturnsFalse()
    {
        // Arrange: the same message and no metadata, only the type differs
        var sut = Found() with { Error = new TestError("Failed.") };

        // Act
        var result = sut.Equals(Found() with { Error = new OtherTestError("Failed.") });

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentErrorMessage_ReturnsFalse()
    {
        // Act
        var result = Rejected().Equals(Rejected(message: "Rejected: WL-107."));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentErrorMetadataValue_ReturnsFalse()
    {
        // Act & Assert: the same message, another code or proof
        Rejected().Equals(Rejected(code: "WL-107")).ShouldBeFalse();
        NotFound().Equals(NotFound(requestId: "K4oSY-99b0ck2")).ShouldBeFalse();
    }

    [Fact]
    public void Equals_ErrorWithAnotherMetadataKey_ReturnsFalse()
    {
        // Arrange: the same type and message, one entry under another key
        var sut = Found() with { Error = new TestError("Failed.", ("a", "1")) };

        // Act
        var result = sut.Equals(Found() with { Error = new TestError("Failed.", ("b", "1")) });

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ErrorWithMoreMetadata_ReturnsFalse()
    {
        // Arrange: every entry of the first is in the second, which has one more
        var sut = Found() with { Error = new TestError("Failed.", ("a", "1")) };
        var other = Found() with { Error = new TestError("Failed.", ("a", "1"), ("b", "2")) };

        // Act & Assert
        sut.Equals(other).ShouldBeFalse();
        other.Equals(sut).ShouldBeFalse();
    }

    [Fact]
    public void Equals_ErrorWithTheSameMetadata_ReturnsTrue()
    {
        // Arrange
        var sut = Found() with { Error = new TestError("Failed.", ("a", "1"), ("b", null)) };

        // Act
        var result = sut.Equals(Found() with { Error = new TestError("Failed.", ("b", null), ("a", "1")) });

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_SameValuesInDifferentInstances_ReturnsSameHashCode()
    {
        // Act & Assert
        Found().GetHashCode().ShouldBe(Found().GetHashCode());
        Rejected().GetHashCode().ShouldBe(Rejected().GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_ReturnsAnotherHashCode()
    {
        // Act
        var result = Found().GetHashCode();

        // Assert
        result.ShouldNotBe((Found() with { Identifier = "5260250274" }).GetHashCode());
        result.ShouldNotBe((Found() with { Subjects = [new Subject("Nazwa Firmy 1")] }).GetHashCode());
        result.ShouldNotBe((Found() with { Error = Rejected().Error }).GetHashCode());
        Rejected().GetHashCode().ShouldNotBe(Rejected(message: "Rejected: WL-107.").GetHashCode());
    }

    // Errors of an app's own types, whose metadata the test chooses.
    private class TestError : OpenUrzednikError
    {
        public TestError(string message, params (string Key, object? Value)[] metadata)
            : base("test", message)
        {
            foreach (var (key, value) in metadata)
                AddMetadata(key, value);
        }

        protected override OpenUrzednikException CreateException() => throw new NotSupportedException();
    }

    private sealed class OtherTestError(string message) : TestError(message);
}
