using Microsoft.Extensions.Logging;

using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Extensions.Logging.Tests;

public sealed partial class OpenUrzednikLoggerTest
{
    [Fact]
    public void Log_NoValues_WritesTemplateAsMessageAndOriginalFormat()
    {
        // Act
        CreateSut().Log(OpenUrzednikLogLevel.Information, null, "Client created");

        // Assert
        var entry = _logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Information);
        entry.Message.ShouldBe("Client created");
        entry.State.ShouldBe([new("{OriginalFormat}", "Client created")]);
    }

    [Fact]
    public void Log_OneValue_WritesStructuredState()
    {
        // Act
        CreateSut().Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for {operation}", "operation", "GetAsync");

        // Assert
        var entry = _logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Debug);
        entry.Message.ShouldBe("Validation failed for GetAsync");
        entry.State.ShouldBe([new("operation", "GetAsync"), new("{OriginalFormat}", "Validation failed for {operation}")]);
    }

    [Fact]
    public void Log_TwoValues_WritesStructuredStateAndException()
    {
        // Arrange
        var exception = new HttpRequestException("Connection refused");

        // Act
        CreateSut().Log(OpenUrzednikLogLevel.Warning, exception, "{provider} request to {path} failed", "provider", "NBP API", "path", "cenyzlota");

        // Assert
        var entry = _logger.Entries.ShouldHaveSingleItem();
        entry.Exception.ShouldBeSameAs(exception);
        entry.Message.ShouldBe("NBP API request to cenyzlota failed");
        entry.State.ShouldBe([new("provider", "NBP API"), new("path", "cenyzlota"), new("{OriginalFormat}", "{provider} request to {path} failed")]);
    }

    [Fact]
    public void Log_ThreeValues_KeepsValueTypes()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(10);

        // Act
        CreateSut().Log(OpenUrzednikLogLevel.Warning, null, "{provider} did not respond to {path} within {timeout}",
            "provider", "NBP API", "path", "cenyzlota", "timeout", timeout);

        // Assert
        var entry = _logger.Entries.ShouldHaveSingleItem();
        entry.Message.ShouldBe("NBP API did not respond to cenyzlota within 00:00:10");
        entry.State[2].ShouldBe(new("timeout", timeout));
    }

    [Fact]
    public void Log_PropertyList_WritesStructuredState()
    {
        // Arrange
        KeyValuePair<string, object?>[] properties = [new("a", 1), new("b", 2), new("c", 3), new("d", 4)];

        // Act
        CreateSut().Log(OpenUrzednikLogLevel.Error, null, "{a} {b} {c} {d}", properties);

        // Assert
        var entry = _logger.Entries.ShouldHaveSingleItem();
        entry.Message.ShouldBe("1 2 3 4");
        entry.State.Count.ShouldBe(5);
        entry.State[3].ShouldBe(new("d", 4));
    }

    [Fact]
    public void Log_NullPropertyList_ThrowsArgumentNullException()
    {
        // Act
        var exception = Should.Throw<ArgumentNullException>(() => CreateSut().Log(OpenUrzednikLogLevel.Error, null, "Message", null!));

        // Assert
        exception.ParamName.ShouldBe("properties");
    }

    [Fact]
    public void Log_LevelBelowLoggerMinimum_WritesNothing()
    {
        // Arrange
        _logger.MinimumLevel = LogLevel.Information;
        var sut = CreateSut();

        // Act
        sut.Log(OpenUrzednikLogLevel.Debug, null, "Message");
        sut.Log(OpenUrzednikLogLevel.Debug, null, "{a}", "a", 1);
        sut.Log(OpenUrzednikLogLevel.Debug, null, "{a} {b}", "a", 1, "b", 2);
        sut.Log(OpenUrzednikLogLevel.Debug, null, "{a} {b} {c}", "a", 1, "b", 2, "c", 3);
        sut.Log(OpenUrzednikLogLevel.Debug, null, "{a}", [new KeyValuePair<string, object?>("a", 1)]);

        // Assert
        _logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Log_LevelNone_WritesNothing()
    {
        // Act
        CreateSut().Log(OpenUrzednikLogLevel.None, null, "Message");

        // Assert
        _logger.Entries.ShouldBeEmpty();
    }
}
