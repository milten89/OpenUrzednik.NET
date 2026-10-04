using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.TestCommon.Extensions;

namespace OpenUrzednik.Core.Tests.Extensions;

public partial class IOpenUrzednikSpanExtensionsTest
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
}
