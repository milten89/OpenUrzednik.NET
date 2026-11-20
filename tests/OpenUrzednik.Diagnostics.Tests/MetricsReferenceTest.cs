using System.Diagnostics.Metrics;
using System.Net;
using System.Text.RegularExpressions;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

// ADR-0025 confirmation: the metric tables in the READMEs list what a client records, by name, kind, unit and tags.
public sealed class MetricsReferenceTest
{
    private const string InstrumentName = "openurzednik.client.operation.duration";

    public static TheoryData<string> References => ["src/OpenUrzednik.Nbp/README.md", "src/OpenUrzednik.Diagnostics/README.md"];

    [Theory]
    [MemberData(nameof(References))]
    public async Task Readme_MetricsTable_MatchesWhatAClientRecords(string readme)
    {
        // Arrange: a failing call, so every tag the instrument can carry is recorded (error.type only on failure)
        var (unit, tags) = ReadRow(readme);
        var meterName = $"OpenUrzednik.Tests.{Guid.NewGuid():N}";
        using var recorder = new MeterRecorder(meterName);
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var sut = new NbpGoldPriceClient(httpClient, null, null, new OpenUrzednikTelemetry { Metrics = MeterMetrics.GetShared(meterName) });

        // Act
        await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        var measurement = recorder.Measurements.ShouldHaveSingleItem();
        measurement.Instrument.Name.ShouldBe(InstrumentName);
        measurement.Instrument.ShouldBeOfType<Histogram<double>>();
        measurement.Instrument.Unit.ShouldBe(unit);
        measurement.Tags.Select(tag => tag.Key).ShouldBe(tags, ignoreOrder: true);
    }

    // The table row of the instrument: | `name` | histogram, seconds (`s`) | `tag` (note), `tag`, … |
    private static (string Unit, string[] Tags) ReadRow(string readme)
    {
        // Concatenated as RepositoryFiles.PathOf does: Path.Combine would drop the root if the path were absolute.
        var row = File.ReadLines(RepositoryRoot() + Path.DirectorySeparatorChar + readme.Replace('/', Path.DirectorySeparatorChar))
            .Where(line => line.StartsWith($"| `{InstrumentName}` |", StringComparison.Ordinal))
            .ShouldHaveSingleItem($"{readme} lists {InstrumentName} once");
        var cells = row.Split(['|'], StringSplitOptions.RemoveEmptyEntries).Select(cell => cell.Trim()).ToArray();
        cells.Length.ShouldBe(3, row);
        cells[1].ShouldStartWith("histogram");

        var unit = Regex.Match(cells[1], "`([^`]+)`").Groups[1].Value;
        // The notes in parentheses hold examples in backticks too (e.g. `nbp.gold.latest`), so drop them first.
        var tags = Regex.Matches(Regex.Replace(cells[2], @"\([^)]*\)", string.Empty), "`([^`]+)`")
            .Cast<Match>()
            .Select(match => match.Groups[1].Value)
            .ToArray();
        return (unit, tags);
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (directory.EnumerateFiles("OpenUrzednik.slnx").Any())
                return directory.FullName;
        }

        throw new DirectoryNotFoundException($"OpenUrzednik.slnx not found above {AppContext.BaseDirectory}.");
    }
}
