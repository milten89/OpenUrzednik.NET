using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.TestCommon.Extensions;

namespace OpenUrzednik.Core.Tests.Extensions;

public class IOpenUrzednikSpanExtensionsTest
{
    [Fact]
    public void RecordError_WhenNotRecording_DoesNothing()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(false);
        var error = Substitute.For<OpenUrzednikError>(faker.Random.String2(10), faker.Lorem.Sentence());

        // Act
        span.RecordError(error);

        // Assert
        _ = error.DidNotReceive().Code;
        _ = error.DidNotReceive().Message;
    }

    [Fact]
    public void RecordError_WhenRecording_SetsErrorCodeTagAndStatus()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var code = faker.Random.String2(10);
        var message = faker.Lorem.Sentence();
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);
        var error = Substitute.For<OpenUrzednikError>(code, message);

        // Act
        span.RecordError(error);

        // Assert
        _ = error.Received(1).Code;
        _ = error.Received(1).Message;
        span.Received(1).SetTag("error.code", code);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, message);
    }

    [Fact]
    public void RecordErrors_EmptyList_DoesNothing()
    {
        // Arrange
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);

        // Act
        span.RecordErrors([]);

        // Assert
        span.DidNotReceive().AddEvent(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        span.DidNotReceive().SetStatus(Arg.Any<OpenUrzednikSpanStatus>(), Arg.Any<string>());
    }

    [Fact]
    public void RecordErrors_MultipleErrors_AddsEventPerErrorAndAggregateMessage()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var errorCount = faker.Random.Int(3, 10);
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);
        var errors = new List<OpenUrzednikError>();
        for (int i = 0; i < errorCount; i++)
            errors.Add(Substitute.For<OpenUrzednikError>(faker.Random.String2(10), faker.Lorem.Sentence()));

        // Act
        span.RecordErrors(errors);

        // Assert
        span.Received(errorCount).AddEvent(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, $"{errorCount} errors occurred");
        foreach (var error in errors)
        {
            _ = error.Received(1).Code;
            _ = error.DidNotReceive().Message;
        }
    }

    [Fact]
    public void RecordErrors_SingleError_UsesErrorMessageDirectly()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var code = faker.Random.String2(10);
        var message = faker.Lorem.Sentence();
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);
        var error = Substitute.For<OpenUrzednikError>(code, message);

        // Act
        span.RecordErrors([error]);

        // Assert
        _ = error.Received(1).Code;
        _ = error.Received(1).Message;
        span.Received(1).AddEvent("error", "error.code", code);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, message);
    }
}
