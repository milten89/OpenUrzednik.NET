using Microsoft.Extensions.Logging;

using NSubstitute;

using OpenUrzednik.Nbp.Gold;

using Shouldly;

namespace OpenUrzednik.Extensions.Logging.Tests;

public sealed partial class OpenUrzednikLoggerFactoryExtensionsTest
{
    [Fact]
    public void CreateOpenUrzednikLogger_Client_UsesFullTypeNameAsCategory()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var logger = new RecordingLogger();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        // Act
        var sut = loggerFactory.CreateOpenUrzednikLogger<NbpGoldPriceClient>();
        sut.Log(Core.Telemetry.OpenUrzednikLogLevel.Information, null, "Message");

        // Assert
        loggerFactory.Received(1).CreateLogger("OpenUrzednik.Nbp.Gold.NbpGoldPriceClient");
        logger.Entries.ShouldHaveSingleItem().Message.ShouldBe("Message");
    }

    [Fact]
    public void CreateOpenUrzednikLogger_NullFactory_ThrowsArgumentNullException()
    {
        // Act
        var exception = Should.Throw<ArgumentNullException>(() => ((ILoggerFactory)null!).CreateOpenUrzednikLogger<NbpGoldPriceClient>());

        // Assert
        exception.ParamName.ShouldBe("loggerFactory");
    }
}
