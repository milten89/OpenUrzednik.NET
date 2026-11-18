using System.Collections;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

// TestCommon's leak check (ADR-0003): every place an identifier can reach an app is searched, and a masked value passes. The NIP
// and the account number are fake (the NIP's check digit is valid); their digit runs can't appear by chance in the texts below.
public class IdentifierLeakTest
{
    private const string Nip = "7368125416";
    private const string Masked = "******5416";
    private const string Account = "61109010140000071219812874";
    private static readonly OpenUrzednikEventId Event = new(1, "Test");

    private static readonly Dictionary<string, Func<RecordingTelemetry, OpenUrzednikResult>> Leaks = new()
    {
        ["log template"] = telemetry => Log(telemetry, $"Searching {Nip}."),
        ["log property"] = telemetry => Do(() => telemetry.Log(OpenUrzednikLogLevel.Information, Event, null, "Searching {nip}.", "nip", Nip)),
        ["log property list"] = telemetry => Do(() => telemetry.Log(OpenUrzednikLogLevel.Information, Event, null, "Searching.",
            [new KeyValuePair<string, object?>("nips", new[] { Masked, Nip })])),
        ["log property dictionary"] = telemetry => Do(() => telemetry.Log(OpenUrzednikLogLevel.Information, Event, null, "Searching {query}.",
            "query", new Dictionary<string, string[]> { ["nips"] = [Nip] })),
        ["logged exception"] = telemetry => Do(() => telemetry.Log(OpenUrzednikLogLevel.Error, Event, new InvalidOperationException($"Bad {Nip}"), "Failed.")),
        ["inner exception"] = telemetry => Do(() => telemetry.Log(OpenUrzednikLogLevel.Error, Event,
            new InvalidOperationException("Outer.", new FormatException($"Inner {Nip}")), "Failed.")),
        ["second exception of an aggregate"] = telemetry => Do(() => telemetry.StartSpan("op").RecordException(
            new AggregateException(new InvalidOperationException("First."), WithData(new InvalidOperationException("Second."), "nip", Nip)))),
        ["exception data value"] = telemetry => Do(() => telemetry.StartSpan("op").RecordException(WithData(new InvalidOperationException("Bad."), "nip", Nip))),
        ["exception data key"] = telemetry => Do(() => telemetry.StartSpan("op").RecordException(WithData(new InvalidOperationException("Bad."), Nip, "found"))),
        ["exception data table"] = telemetry => Do(() => telemetry.StartSpan("op").RecordException(
            WithData(new InvalidOperationException("Bad."), "query", new Hashtable { ["nip"] = Nip }))),
        ["errors of a validation exception"] = telemetry => Do(() => telemetry.Log(OpenUrzednikLogLevel.Error, Event, MultiErrorException(), "Failed.")),
        ["span name"] = telemetry => Do(() => telemetry.StartSpan($"search {Nip}")),
        ["span tag"] = telemetry => Do(() => telemetry.StartSpan("op").SetTag("url.path", $"/api/search/nip/{Nip}")),
        ["span tag of a number"] = telemetry => Do(() => telemetry.StartSpan("op").SetTag("nip", 7368125416L)),
        ["span tag of characters"] = telemetry => Do(() => telemetry.StartSpan("op").SetTag("nip", Nip.ToCharArray())),
        ["span status"] = telemetry => Do(() => telemetry.StartSpan("op").SetStatus(OpenUrzednikSpanStatus.Error, $"No subject {Nip}")),
        ["span event"] = telemetry => Do(() => telemetry.StartSpan("op").AddEvent($"lookup {Nip}")),
        ["span event tag"] = telemetry => Do(() => telemetry.StartSpan("op").AddEvent("lookup", "nip", Nip)),
        ["error message"] = _ => OpenUrzednikResult.Failure(new TestError($"No subject {Nip}.")),
        ["error metadata"] = _ => OpenUrzednikResult.Failure(new ValidationError<string>("Invalid NIP.", "nip", "nip", Nip)),
        ["exception behind an error"] = _ => OpenUrzednikResult.Failure(new ConnectionFailedError("Connection failed.", new HttpRequestException($"Failed for {Nip}."))),
        ["a formatted identifier"] = telemetry => Log(telemetry, "Searching 736-812-54-16."),
        ["a spaced identifier"] = telemetry => Log(telemetry, "Searching 736 812 54 16."),
        ["a five-digit prefix"] = telemetry => Log(telemetry, "Searching 73681*****."),
        ["five digits in the middle"] = telemetry => Log(telemetry, "Searching **68125***."),
    };

    public static TheoryData<string> LeakPlaces() => new(Leaks.Keys);

    [Theory]
    [MemberData(nameof(LeakPlaces))]
    public void ShouldNotLeak_IdentifierInAnyPlace_Throws(string place)
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var result = Leaks[place](telemetry);

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => result.ShouldNotLeak(telemetry, Nip)).Message.ShouldContain(Nip);
    }

    [Theory]
    [InlineData("736-812-54-16")]
    [InlineData("PL7368125416")]
    public void ShouldNotLeak_IdentifierGivenFormatted_SearchesItsDigits(string identifier)
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var result = Log(telemetry, $"Searching {Nip}.");

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => result.ShouldNotLeak(telemetry, identifier));
    }

    [Theory]
    [InlineData("Paying to 61 1090 1014 0000 0712 1981 2874.")]
    [InlineData("Paying to PL61109010140000071219812874.")]
    [InlineData("Paying to pl61109010140000071219812874.")]
    public void ShouldNotLeak_AccountNumberGroupedOrPrefixed_Throws(string text)
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var result = Log(telemetry, text);

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => result.ShouldNotLeak(telemetry, $"PL{Account}"));
    }

    [Fact]
    public void ShouldNotLeak_MaskedEverywhere_DoesNotThrow()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        Log(telemetry, $"Searching {Masked} at 2026-01-05T10:15:30Z, status 404.");
        telemetry.Log(OpenUrzednikLogLevel.Debug, Event, null, "Searching {nip}.", "nip", Masked);
        var span = telemetry.StartSpan("mf_white_list.search.by_nip");
        span.SetTag("url.path", "/api/search/nip/{nip}");
        span.SetTag("http.response.status_code", 404);
        span.SetStatus(OpenUrzednikSpanStatus.Error, "Not found");
        span.RecordException(WithData(new InvalidOperationException($"Bad {Masked}."), "nip", Masked));
        var result = OpenUrzednikResult.Failure(new ValidationError<string>($"Invalid NIP {Masked}.", "nip", "nip", Masked));

        // Act & Assert
        Should.NotThrow(() => result.ShouldNotLeak(telemetry, Nip));
    }

    [Fact]
    public void ShouldNotLeak_Leak_NamesThePlace()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        telemetry.StartSpan("op").SetTag("url.path", $"/api/search/nip/{Nip}");

        // Act
        var exception = Should.Throw<ShouldAssertException>(() => OpenUrzednikResult.Success().ShouldNotLeak(telemetry, Nip));

        // Assert
        exception.Message.ShouldContain("span op: tag url.path");
    }

    [Fact]
    public void ShouldNotLeak_NoIdentifier_ThrowsArgumentException()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();

        // Act & Assert
        Should.Throw<ArgumentException>(() => OpenUrzednikResult.Success().ShouldNotLeak(telemetry)).ParamName.ShouldBe("identifiers");
        Should.Throw<ArgumentException>(() => OpenUrzednikResult.Success().ShouldNotLeak(telemetry, " ")).ParamName.ShouldBe("identifiers");
    }

    [Fact]
    public void ShouldNotLeak_GenericResult_SearchesItsErrors()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var result = OpenUrzednikResult.Failure<int>(new TestError($"No subject {Nip}."));

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => result.ShouldNotLeak(telemetry, Nip));
    }

    // Two validation errors: EnsureSuccess throws one exception whose message names the first, and lists both in Errors.
    private static Exception MultiErrorException()
        => Should.Throw<OpenUrzednikValidationException>(() =>
            OpenUrzednikResult.Failure([new ValidationError("First.", "rule", "a", null), new ValidationError<string>("Second.", "nip", "nip", Nip)]).EnsureSuccess());

    private static OpenUrzednikResult Log(RecordingTelemetry telemetry, string template)
        => Do(() => telemetry.Log(OpenUrzednikLogLevel.Information, Event, null, template));

    private static OpenUrzednikResult Do(Action action)
    {
        action();
        return OpenUrzednikResult.Success();
    }

    private static Exception WithData(Exception exception, object key, object value)
    {
        exception.Data[key] = value;
        return exception;
    }
}
