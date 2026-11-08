using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public class RangeValidationErrorTest
{
    private static readonly DateTime Start = new(2026, 1, 5);
    private static readonly DateTime End = new(2026, 6, 1);

    [Fact]
    public void Ctor_Always_KeepsTheBoundsAndTheirNames()
    {
        // Act
        var error = new RangeValidationError<DateTime>("message", "dateRange", "from", Start, "to", End);

        // Assert
        error.Start.ShouldBe(Start);
        error.End.ShouldBe(End);
        error.StartName.ShouldBe("from");
        error.EndName.ShouldBe("to");
        error.RuleName.ShouldBe("dateRange");
        error.Name.ShouldBeNull();
        error.Code.ShouldBe(ValidationError.ErrorCode);
        error.ShouldBeAssignableTo<ValidationError>();
    }

    [Fact]
    public void Metadata_DateRange_HasBothBoundsAsIsoStringsAndNoValue()
    {
        // Act
        var metadata = new RangeValidationError<DateTime>("message", "dateRange", "from", Start, "to", End).Metadata;

        // Assert
        metadata[ValidationError.RuleNameMetadataKey].ShouldBe("dateRange");
        metadata[RangeValidationError.StartNameMetadataKey].ShouldBe("from");
        metadata[RangeValidationError.StartMetadataKey].ShouldBe("2026-01-05");
        metadata[RangeValidationError.EndNameMetadataKey].ShouldBe("to");
        metadata[RangeValidationError.EndMetadataKey].ShouldBe("2026-06-01");
        metadata.ContainsKey(ValidationError.NameMetadataKey).ShouldBeFalse();
        metadata.ContainsKey(ValidationError.ValueMetadataKey).ShouldBeFalse();
    }

    [Fact]
    public void Metadata_DateRange_ListsTheKeysInTheDocumentedOrder()
    {
        // Act
        var keys = new RangeValidationError<DateTime>("message", "dateRange", "from", Start, "to", End).Metadata.Keys;

        // Assert
        keys.ShouldBe([ValidationError.RuleNameMetadataKey, RangeValidationError.StartNameMetadataKey, RangeValidationError.StartMetadataKey,
            RangeValidationError.EndNameMetadataKey, RangeValidationError.EndMetadataKey]);
    }

    [Fact]
    public void Metadata_NumericRange_KeepsTheNumbers()
    {
        // Act
        var error = new RangeValidationError<int>("message", "range", "min", 5, "max", 1);

        // Assert
        error.Metadata[RangeValidationError.StartMetadataKey].ShouldBe(5);
        error.Metadata[RangeValidationError.EndMetadataKey].ShouldBe(1);
        error.ShouldFollowTheMetadataRule();
    }

    // A typed range leaves out a bound with no form, so these show each type converted (ValidationError keeps such values as passed).
    [Fact]
    public void Metadata_BoundsOfEachAllowedType_AreKept()
    {
        // Assert
        new RangeValidationError<string>("m", "r", "a", "x", "b", "y").Metadata[RangeValidationError.StartMetadataKey].ShouldBe("x");
        new RangeValidationError<bool>("m", "r", "a", true, "b", false).Metadata[RangeValidationError.StartMetadataKey].ShouldBe(true);
        new RangeValidationError<long>("m", "r", "a", 5L, "b", 1L).Metadata[RangeValidationError.StartMetadataKey].ShouldBe(5L);
        new RangeValidationError<double>("m", "r", "a", 1.5, "b", 0.5).Metadata[RangeValidationError.StartMetadataKey].ShouldBe(1.5);
    }

    [Fact]
    public void Metadata_ArrayBoundsOfAnAllowedType_AreKeptAsIs()
    {
        // Arrange
        int[] start = [1, 2];
        int[] end = [3];

        // Act
        var error = new RangeValidationError<int[]>("m", "r", "a", start, "b", end);

        // Assert
        error.Metadata[RangeValidationError.StartMetadataKey].ShouldBeSameAs(start);
        error.Metadata[RangeValidationError.EndMetadataKey].ShouldBeSameAs(end);
    }

    [Fact]
    public void Metadata_ArrayBounds_AreConvertedToStringArrays()
    {
        // Act
        var error = new RangeValidationError<DateTime[]>("m", "r", "a", [new DateTime(2026, 1, 5)], "b", [new DateTime(2026, 2, 1)]);

        // Assert
        error.Metadata[RangeValidationError.StartMetadataKey].ShouldBeOfType<string[]>().ShouldBe(["2026-01-05"]);
        error.Metadata[RangeValidationError.EndMetadataKey].ShouldBeOfType<string[]>().ShouldBe(["2026-02-01"]);
    }

    [Fact]
    public void Metadata_BoundsWithoutAForm_AreLeftOut()
    {
        // Act
        var error = new RangeValidationError<(int, int)>("message", "range", "a", (1, 2), "b", (3, 4));

        // Assert
        error.Metadata.ContainsKey(RangeValidationError.StartMetadataKey).ShouldBeFalse();
        error.Metadata.ContainsKey(RangeValidationError.EndMetadataKey).ShouldBeFalse();
        error.Metadata[RangeValidationError.StartNameMetadataKey].ShouldBe("a");
        error.ShouldFollowTheMetadataRule();
    }

    [Fact]
    public void Metadata_KeyConstants_HaveTheDocumentedValues()
    {
        // Assert
        RangeValidationError.StartNameMetadataKey.ShouldBe("startName");
        RangeValidationError.StartMetadataKey.ShouldBe("start");
        RangeValidationError.EndNameMetadataKey.ShouldBe("endName");
        RangeValidationError.EndMetadataKey.ShouldBe("end");
    }

    [Theory]
    [InlineData("", "to", "startName")]
    [InlineData(" ", "to", "startName")]
    [InlineData("from", "", "endName")]
    [InlineData("from", " ", "endName")]
    public void Ctor_EmptyOrWhiteSpaceBoundName_ThrowsArgumentException(string startName, string endName, string parameter)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new RangeValidationError<int>("message", "range", startName, 1, endName, 2))
            .ParamName.ShouldBe(parameter);
    }

    [Fact]
    public void ToException_Always_IsAValidationExceptionWithoutAValue()
    {
        // Act
        var exception = new RangeValidationError<DateTime>("message", "dateRange", "from", Start, "to", End).ToException();

        // Assert
        var validation = exception.ShouldBeOfType<OpenUrzednikValidationException>();
        validation.Data["ruleName"].ShouldBe("dateRange");
        validation.Data.Contains("name").ShouldBeFalse();
        validation.Data.Contains("value").ShouldBeFalse();
    }
}
