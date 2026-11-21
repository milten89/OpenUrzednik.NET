using System.Net;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Search;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

// A successful batch's failed entries (ADR-0023): one Debug entry with the counts, never an identifier.
public sealed partial class WhiteListRequestPipelineTest
{
    private const string Template = "MF White List API answered {operation} with {notOnList} entries not on the list and {rejected} rejected";

    private static BatchSearchEntry Found(string identifier) => new(identifier, [new Subject("Nazwa Firmy 1")], null);

    private static BatchSearchEntry NotOnList(string identifier)
        => new(identifier, [], new WhiteListNotFoundError("Not on the list.", "rd9zq-99alll1", Answered));

    private static BatchSearchEntry Rejected(string identifier)
        => new(identifier, [], new WhiteListQueryRejectedError("Rejected.", "WL-115"));

    private WhiteListRequestPipeline CreateSut(IOpenUrzednikLogger logger)
        => CreateSut(Json(HttpStatusCode.OK, "{}"), out _, new ClientTelemetry(logger));

    [Fact]
    public void LogFailedEntries_EveryEntrySucceeded_LogsNothing()
    {
        // Arrange
        var logger = CreateLogger();

        // Act
        CreateSut(logger).LogFailedEntries("SearchByNipsAsync", [Found("3245174504"), Found("5260250274")]);

        // Assert
        logger.DidNotReceive().Log(Arg.Any<OpenUrzednikLogLevel>(), WhiteListEvents.NotOnList, Arg.Any<Exception?>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int>());
    }

    [Fact]
    public void LogFailedEntries_EntriesNotOnTheList_LogsTheirCount()
    {
        // Arrange
        var logger = CreateLogger();

        // Act
        CreateSut(logger).LogFailedEntries("SearchByNipsAsync", [NotOnList("3245174504"), Found("5260250274"), NotOnList("1854510877")]);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.NotOnList, null, Template,
            "operation", "SearchByNipsAsync", "notOnList", 2, "rejected", 0);
    }

    [Fact]
    public void LogFailedEntries_RejectedEntries_LogsTheirCount()
    {
        // Arrange
        var logger = CreateLogger();

        // Act
        CreateSut(logger).LogFailedEntries("SearchByRegonsAsync", [Found("216973362"), Rejected("123456789")]);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.NotOnList, null, Template,
            "operation", "SearchByRegonsAsync", "notOnList", 0, "rejected", 1);
    }

    [Fact]
    public void LogFailedEntries_BothKinds_LogsBothCounts()
    {
        // Arrange
        var logger = CreateLogger();

        // Act
        CreateSut(logger).LogFailedEntries("SearchByNipsAsync", [NotOnList("5260250274"), Rejected("1234567890"), Rejected("1234567891")]);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.NotOnList, null, Template,
            "operation", "SearchByNipsAsync", "notOnList", 1, "rejected", 2);
    }

    [Fact]
    public void LogFailedEntries_DebugDisabled_LogsNothing()
    {
        // Arrange
        var logger = Substitute.For<IOpenUrzednikLogger>();
        logger.IsEnabled(OpenUrzednikLogLevel.Debug).Returns(false);

        // Act
        CreateSut(logger).LogFailedEntries("SearchByNipsAsync", [NotOnList("5260250274")]);

        // Assert
        logger.DidNotReceive().Log(Arg.Any<OpenUrzednikLogLevel>(), WhiteListEvents.NotOnList, Arg.Any<Exception?>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int>());
    }
}
