using OpenUrzednik.Dates;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Dates;

public class DateOnlyExtensionsTest
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("pl-PL")]
    [InlineData("th-TH")]
    [InlineData("ar-SA")]
    [InlineData("fa-IR")]
    public void ToIso8601String_AnyCulture_ReturnsGregorianIsoDate(string culture)
    {
        // Arrange
        using var _ = new CultureScope(culture);

        // Act
        var result = new DateOnly(2026, 1, 5).ToIso8601String();

        // Assert
        result.ShouldBe("2026-01-05");
    }

    [Fact]
    public void ToPublicDate_AnyDate_ReturnsMidnightWithUnspecifiedKind()
    {
        // Act
        var result = new DateOnly(2026, 1, 5).ToPublicDate();

        // Assert
        result.ShouldBe(new DateTime(2026, 1, 5));
        result.TimeOfDay.ShouldBe(TimeSpan.Zero);
        result.Kind.ShouldBe(DateTimeKind.Unspecified);
    }

#if !NET
    // On .NET Framework DateOnly is DateTime, so a value can carry a time of day and a Kind; ToPublicDate drops both (ADR-0005).
    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    public void ToPublicDate_DateTimeWithTimeAndKind_ReturnsMidnightWithUnspecifiedKind(DateTimeKind kind)
    {
        // Arrange
        var value = new DateTime(2026, 1, 5, 23, 59, 59, kind).AddTicks(9_999_999);

        // Act
        var result = value.ToPublicDate();

        // Assert
        result.ShouldBe(new DateTime(2026, 1, 5));
        result.Kind.ShouldBe(DateTimeKind.Unspecified);
    }
#endif
}
