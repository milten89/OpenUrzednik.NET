using System.Xml;

using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

// Durations are ISO 8601 strings written by Core's own formatter; System.Xml's XmlConvert writes the same text (ADR-0021).
public class DurationMetadataTest
{
    public static TheoryData<TimeSpan, string> Durations => new()
    {
        { TimeSpan.Zero, "PT0S" },
        { TimeSpan.FromSeconds(30), "PT30S" },
        { TimeSpan.FromMinutes(90), "PT1H30M" },
        { new TimeSpan(1, 2, 0, 0), "P1DT2H" },
        { TimeSpan.FromDays(3), "P3D" },
        { TimeSpan.FromMilliseconds(500), "PT0.5S" },
        { TimeSpan.FromTicks(1), "PT0.0000001S" },
        { TimeSpan.FromSeconds(-30), "-PT30S" },
        { -new TimeSpan(1, 0, 0, 0, 500), "-P1DT0.5S" },
        { TimeSpan.FromTicks(-1), "-PT0.0000001S" },
        { TimeSpan.MaxValue, "P10675199DT2H48M5.4775807S" },
        { TimeSpan.MinValue, "-P10675199DT2H48M5.4775808S" },
        { new TimeSpan(0, 5, 0, 7), "PT5H7S" },
        { new TimeSpan(2, 0, 3, 0), "P2DT3M" },
    };

    [Theory]
    [MemberData(nameof(Durations))]
    public void Metadata_Duration_IsTheIsoText(TimeSpan duration, string expected)
    {
        // Act
        var form = Form(duration);

        // Assert
        form.ShouldBe(expected);
        form.ShouldBe(XmlConvert.ToString(duration));
    }

    [Fact]
    public void Metadata_RandomDurations_MatchXmlConvert()
    {
        // Arrange: seeded, so a failure repeats
        var random = new Random(20261108);
        var buffer = new byte[8];

        for (var i = 0; i < 5000; i++)
        {
            random.NextBytes(buffer);
            var ticks = BitConverter.ToInt64(buffer, 0);
            // Every scale: whole ranges, days, seconds, ticks.
            var duration = TimeSpan.FromTicks((i % 4) switch
            {
                0 => ticks,
                1 => ticks % (TimeSpan.TicksPerDay * 400),
                2 => ticks % TimeSpan.TicksPerMinute,
                _ => ticks % 10_000,
            });

            // Act & Assert
            Form(duration).ShouldBe(XmlConvert.ToString(duration), $"ticks {duration.Ticks}");
        }
    }

    private static object? Form(TimeSpan duration)
        => new ValidationError<TimeSpan>("message", "rule", null, duration).Metadata[ValidationError.ValueMetadataKey];
}
