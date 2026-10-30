using System.Reflection;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Nbp.Common;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests;

// NbpLogEvents has only constants, so its tests check them against the events the clients log.
public class NbpLogEventsTest
{
    private static Dictionary<string, int> PublicIds()
        => typeof(NbpLogEvents).GetFields(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(field => field.Name, field => (int)field.GetRawConstantValue()!);

    private static List<OpenUrzednikEventId> LoggedEvents(Type type)
        => [.. type.GetFields(BindingFlags.NonPublic | BindingFlags.Static).Select(field => (OpenUrzednikEventId)field.GetValue(null)!)];

    [Fact]
    public void Constants_HttpEvents_HaveMatchingIdsAndNames()
    {
        // Arrange
        var ids = PublicIds();

        // Act & Assert
        foreach (var logged in LoggedEvents(typeof(RestLogEvents)))
        {
            ids.ShouldContainKey(logged.Name!);
            ids[logged.Name!].ShouldBe(logged.Id, logged.Name);
        }
    }

    [Fact]
    public void Constants_NbpEvents_HaveMatchingIdsAndNames()
    {
        // Arrange
        var ids = PublicIds();

        // Act & Assert
        foreach (var logged in LoggedEvents(typeof(NbpEvents)))
        {
            ids.ShouldContainKey(logged.Name!);
            ids[logged.Name!].ShouldBe(logged.Id, logged.Name);
        }
    }

    [Fact]
    public void Constants_All_AreLoggedAndUnique()
    {
        // Arrange
        var ids = PublicIds();
        var logged = LoggedEvents(typeof(RestLogEvents)).Concat(LoggedEvents(typeof(NbpEvents))).ToList();

        // Act & Assert
        ids.Count.ShouldBe(logged.Count);
        ids.Values.Distinct().Count().ShouldBe(ids.Count);
    }
}
