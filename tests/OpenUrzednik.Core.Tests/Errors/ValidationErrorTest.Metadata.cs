using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

// The metadata forms of ADR-0022, through the value of a ValidationError: the 1.0 constructor (object) and ValidationError<T>.
public partial class ValidationErrorTest
{
    public static TheoryData<object, object> ValuesAndForms => new()
    {
        { "text", "text" },
        { true, true },
        { 42, 42 },
        { 42L, 42L },
        { 1.5, 1.5 },
        { (byte)7, 7 },
        { (sbyte)-7, -7 },
        { (short)-300, -300 },
        { (ushort)300, 300 },
        { 7u, 7L },
        { 1.5f, 1.5 },
        { new DateTime(2026, 1, 5), "2026-01-05" },
        { new DateTime(2026, 1, 5, 14, 30, 0), "2026-01-05T14:30:00" },
        { new DateTime(2026, 1, 5, 14, 30, 0, 500), "2026-01-05T14:30:00.5" },
        { new DateTime(2026, 1, 5, 14, 30, 0, DateTimeKind.Utc), "2026-01-05T14:30:00Z" },
        { new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), "2026-01-05T00:00:00Z" },
        { new DateTimeOffset(2026, 1, 5, 14, 30, 0, TimeSpan.FromHours(1)), "2026-01-05T14:30:00+01:00" },
        { new DateTimeOffset(2026, 7, 5, 14, 30, 0, TimeSpan.FromHours(-5)), "2026-07-05T14:30:00-05:00" },
        { TimeSpan.FromSeconds(30), "PT30S" },
        { TimeSpan.FromMilliseconds(500), "PT0.5S" },
        { new TimeSpan(1, 2, 0, 0), "P1DT2H" },
        { DayOfWeek.Monday, "Monday" },
        { 1234.50m, "1234.50" },
        { 79228162514264337593543950335m, "79228162514264337593543950335" },
        { 'x', "x" },
        { new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"), "0f8fad5b-d9cb-469f-a165-70867728950e" },
        { 7ul, 7L },
        { (ulong)long.MaxValue, long.MaxValue },
        { ulong.MaxValue, "18446744073709551615" },
#if NET
        { new DateOnly(2026, 1, 5), "2026-01-05" },
        { new TimeOnly(14, 30), "14:30:00" },
        { new TimeOnly(14, 30, 5, 250), "14:30:05.25" },
#endif
    };

    [Theory]
    [MemberData(nameof(ValuesAndForms))]
    public void Metadata_ValueThroughOneZeroConstructor_HasItsMetadataForm(object value, object form)
    {
        // Act
        var metadata = new ValidationError("message", "rule", "name", value).Metadata;

        // Assert
        metadata[ValidationError.ValueMetadataKey].ShouldBe(form);
        metadata[ValidationError.ValueMetadataKey]!.GetType().ShouldBe(form.GetType());
    }

    [Fact]
    public void Metadata_LocalDateTime_KeepsItsOffset()
    {
        // Act
        var form = new ValidationError("message", "rule", "name", new DateTime(2026, 1, 5, 14, 30, 0, DateTimeKind.Local))
            .Metadata[ValidationError.ValueMetadataKey].ShouldBeOfType<string>();

        // Assert
        form.ShouldStartWith("2026-01-05T14:30:00");
        form.ShouldMatch(@"[+-]\d\d:\d\d$");
    }

    [Fact]
    public void Metadata_LocalMidnight_IsADateTimeWithOffset()
    {
        // Act
        var form = new ValidationError("message", "rule", "name", new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Local))
            .Metadata[ValidationError.ValueMetadataKey].ShouldBeOfType<string>();

        // Assert
        form.ShouldMatch(@"^2026-01-05T00:00:00[+-]\d\d:\d\d$");
    }

    [Fact]
    public void Metadata_ArrayOfAllowedType_IsKeptAsIs()
    {
        // Arrange
        int[] numbers = [1, 2];
        string[] texts = ["a", "b"];
        bool[] flags = [true];
        long[] longs = [1L];
        double[] doubles = [1.5];

        // Assert
        new ValidationError("m", "rule", null, numbers).Metadata[ValidationError.ValueMetadataKey].ShouldBeSameAs(numbers);
        new ValidationError("m", "rule", null, texts).Metadata[ValidationError.ValueMetadataKey].ShouldBeSameAs(texts);
        new ValidationError("m", "rule", null, flags).Metadata[ValidationError.ValueMetadataKey].ShouldBeSameAs(flags);
        new ValidationError("m", "rule", null, longs).Metadata[ValidationError.ValueMetadataKey].ShouldBeSameAs(longs);
        new ValidationError("m", "rule", null, doubles).Metadata[ValidationError.ValueMetadataKey].ShouldBeSameAs(doubles);
    }

    [Fact]
    public void Metadata_ArrayOfDatesAndEnums_BecomesAStringArray()
    {
        // Arrange
        object[] mixed = [new DateTime(2026, 1, 5), TimeSpan.FromSeconds(30), DayOfWeek.Friday, 1.5m];

        // Act
        var form = new ValidationError("m", "rule", null, mixed).Metadata[ValidationError.ValueMetadataKey];

        // Assert
        form.ShouldBeOfType<string[]>().ShouldBe(["2026-01-05", "PT30S", "Friday", "1.5"]);
    }

    [Fact]
    public void Metadata_ArrayWithAnElementThatIsNotAString_IsKeptAsPassedByTheOneZeroConstructor()
    {
        // Arrange: a number mixed with strings would make the array heterogeneous
        object[] mixed = ["a", 1];

        // Act
        var form = new ValidationError("m", "rule", null, mixed).Metadata[ValidationError.ValueMetadataKey];

        // Assert
        form.ShouldBeSameAs(mixed);
    }

    public static TheoryData<Array> ArraysWithoutAForm()
    {
        var selfReferencing = new object[1];
        selfReferencing[0] = selfReferencing;
        return new TheoryData<Array>
        {
            new int[2, 2],
            new DateTime[1, 1],
            Array.CreateInstance(typeof(DateTime), [1], [1]),
            selfReferencing,
            new object[] { new[] { new DateTime(2026, 1, 5) } },
            new object[] { new DateTime(2026, 1, 5), null! },
        };
    }

    [Theory]
    [MemberData(nameof(ArraysWithoutAForm))]
    public void Metadata_ArrayWithoutAForm_IsKeptAsPassedWithoutThrowing(Array value)
    {
        // Act
        var form = new ValidationError("m", "rule", null, value).Metadata[ValidationError.ValueMetadataKey];

        // Assert
        form.ShouldBeSameAs(value);
    }

    [Fact]
    public void Metadata_ValueWithoutAForm_IsKeptAsPassedByTheOneZeroConstructor()
    {
        // Arrange
        var tuple = (1, 2);

        // Act
        var form = new ValidationError("m", "rule", null, tuple).Metadata[ValidationError.ValueMetadataKey];

        // Assert
        form.ShouldBe(tuple);
    }

    [Fact]
    public void Metadata_RuleNameAndName_AreStrings()
    {
        // Act
        var error = new ValidationError("message", "topCount", "count", 5);

        // Assert
        error.Metadata[ValidationError.RuleNameMetadataKey].ShouldBe("topCount");
        error.Metadata[ValidationError.NameMetadataKey].ShouldBe("count");
        error.RuleName.ShouldBe("topCount");
        error.Name.ShouldBe("count");
        error.ShouldFollowTheMetadataRule();
    }

    [Fact]
    public void Metadata_NullNameAndValue_HaveNoKeys()
    {
        // Act
        var error = new ValidationError("message", "rule", null, null);

        // Assert
        error.Metadata.Keys.ShouldBe([ValidationError.RuleNameMetadataKey]);
        error.Name.ShouldBeNull();
    }

    [Fact]
    public void ToException_OneZeroConstructor_KeepsTheValueAsPassed()
    {
        // Arrange
        var date = new DateTime(2026, 1, 5);

        // Act
        var exception = new ValidationError("message", "rule", "date", date).ToException();

        // Assert
        var validation = exception.ShouldBeOfType<OpenUrzednikValidationException>();
        validation.Data["ruleName"].ShouldBe("rule");
        validation.Data["name"].ShouldBe("date");
        validation.Data["value"].ShouldBe(date);
    }
}
