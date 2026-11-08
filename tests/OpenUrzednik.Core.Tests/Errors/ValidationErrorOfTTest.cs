using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public class ValidationErrorOfTTest
{
    [Fact]
    public void Ctor_Always_KeepsTheValueWithItsType()
    {
        // Act
        var error = new ValidationError<int>("message", "topCount", "count", 300);

        // Assert
        error.Value.ShouldBe(300);
        error.RuleName.ShouldBe("topCount");
        error.Name.ShouldBe("count");
        error.Code.ShouldBe(ValidationError.ErrorCode);
        error.ShouldBeAssignableTo<ValidationError>();
    }

    [Fact]
    public void Metadata_Date_IsAnIsoString()
    {
        // Act
        var error = new ValidationError<DateTime>("message", "date", "date", new DateTime(2026, 1, 5));

        // Assert
        error.Metadata[ValidationError.ValueMetadataKey].ShouldBe("2026-01-05");
        error.ShouldFollowTheMetadataRule();
    }

    [Fact]
    public void Metadata_DateTimeOffsetAndDuration_AreIsoStrings()
    {
        // Assert
        new ValidationError<DateTimeOffset>("m", "rule", null, new DateTimeOffset(2026, 1, 5, 14, 30, 0, TimeSpan.Zero))
            .Metadata[ValidationError.ValueMetadataKey].ShouldBe("2026-01-05T14:30:00+00:00");
        new ValidationError<TimeSpan>("m", "rule", null, TimeSpan.FromMinutes(90))
            .Metadata[ValidationError.ValueMetadataKey].ShouldBe("PT1H30M");
    }

    [Fact]
    public void Metadata_ArrayOfAllowedType_IsKeptAsIs()
    {
        // Arrange
        int[] numbers = [1, 2];

        // Act
        var error = new ValidationError<int[]>("message", "rule", "numbers", numbers);

        // Assert: a typed error leaves out a value it can't convert, so this shows the array converted
        error.Metadata[ValidationError.ValueMetadataKey].ShouldBeSameAs(numbers);
    }

    [Fact]
    public void Metadata_StringValue_IsKept()
    {
        // Act
        var error = new ValidationError<string>("message", "nip", "nip", "******7890");

        // Assert
        error.Metadata[ValidationError.ValueMetadataKey].ShouldBe("******7890");
    }

    [Fact]
    public void Metadata_ValueWithoutAForm_IsKeptAsPassed()
    {
        // Arrange: an app's ValueValidator<T> of its own type kept its value key in 1.0 (ADR-0021)
        var tuple = (1, 2);

        // Act
        var error = new ValidationError<(int, int)>("message", "rule", "pair", tuple);

        // Assert
        error.Metadata[ValidationError.ValueMetadataKey].ShouldBe(tuple);
        error.Value.ShouldBe(tuple);
    }

    [Fact]
    public void Metadata_NullValue_HasNoValueKey()
    {
        // Act
        var error = new ValidationError<string?>("message", "rule", "name", null);

        // Assert
        error.Metadata.ContainsKey(ValidationError.ValueMetadataKey).ShouldBeFalse();
    }

    [Fact]
    public void ToException_Always_PassesTheValue()
    {
        // Act
        var exception = new ValidationError<int>("message", "topCount", "count", 300).ToException();

        // Assert
        var validation = exception.ShouldBeOfType<OpenUrzednikValidationException>();
        validation.Data["ruleName"].ShouldBe("topCount");
        validation.Data["name"].ShouldBe("count");
        validation.Data["value"].ShouldBe(300);
        validation.Error.ShouldBeOfType<ValidationError<int>>();
    }
}
