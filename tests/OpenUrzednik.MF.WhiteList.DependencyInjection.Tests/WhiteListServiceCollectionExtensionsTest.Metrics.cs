using System.Diagnostics.Metrics;

using Microsoft.Extensions.DependencyInjection;

using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection.Tests;

// ADR-0025: the DI registrations pass MeterMetrics, so their clients record on an OpenUrzednik.MF.WhiteList meter on every target, .NET
// Framework included: the container's IMeterFactory meter when it has one (always on .NET, through AddHttpClient), else the
// shared one. Other tests record on meters of that name at the same time, so each test uses its own API host or factory.
public sealed partial class WhiteListServiceCollectionExtensionsTest
{
    [Fact]
    public async Task AddOpenUrzednikMFWhiteList_Client_RecordsOnTheWhiteListMeter()
    {
        // Arrange
        var host = $"{Guid.NewGuid():N}.example.com";
        var services = new ServiceCollection();
        services.AddOpenUrzednikMFWhiteList(options => options.ApiUrl = $"https://{host}/")
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(Json(SearchNipJson)));
        using var provider = services.BuildServiceProvider();
        using var listener = ListenToWhiteListMeter(host, out var operations);

        // Act
        await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        operations.ShouldBe(["mf_white_list.search.nip"]);
    }

    [Fact]
    public async Task AddKeyedOpenUrzednikMFWhiteList_Client_RecordsOnTheWhiteListMeter()
    {
        // Arrange
        var host = $"{Guid.NewGuid():N}.example.com";
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikMFWhiteList("gateway", options => options.ApiUrl = $"https://{host}/")
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(Json(SearchNipJson)));
        using var provider = services.BuildServiceProvider();
        using var listener = ListenToWhiteListMeter(host, out var operations);

        // Act
        await provider.GetRequiredKeyedService<IWhiteListClient>("gateway").SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        operations.ShouldBe(["mf_white_list.search.nip"]);
    }

    [Fact]
    public async Task AddOpenUrzednikMFWhiteList_ContainerWithAMeterFactory_RecordsOnItsMeter()
    {
        // Arrange: a factory of the app's own, registered before AddHttpClient could add the default one
        using var factory = new TestMeterFactory();
        var services = new ServiceCollection();
        services.AddSingleton<IMeterFactory>(factory);
        services.AddOpenUrzednikMFWhiteList().ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(Json(SearchNipJson)));
        services.AddKeyedOpenUrzednikMFWhiteList("gateway").ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(Json(SearchNipJson)));
        using var provider = services.BuildServiceProvider();
        using var listener = ListenToWhiteListInstruments(out var instruments);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act: a default and a keyed client, and another client type
        await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, cancellationToken);
        await provider.GetRequiredKeyedService<IWhiteListClient>("gateway").SearchByNipAsync(Nip, Date, cancellationToken);
        await provider.GetRequiredService<IWhiteListStatusClient>().GetVatStatusByNipAsync(Nip, Date, cancellationToken);

        // Assert: all on one instrument of the factory's meter, so the container's clients share their instruments
        var own = instruments.Where(instrument => ReferenceEquals(instrument.Meter.Scope, factory)).ToList();
        own.Count.ShouldBe(3);
        own.Distinct().ShouldHaveSingleItem().Meter.Name.ShouldBe(WhiteListTelemetry.SourceName);
    }

    [Fact]
    public async Task AddOpenUrzednikMFWhiteList_ContainerWithoutItsOwnFactory_RecordsOnTheDefaultOrTheSharedMeter()
    {
        // Arrange
        var host = $"{Guid.NewGuid():N}.example.com";
        var services = new ServiceCollection();
        services.AddOpenUrzednikMFWhiteList(options => options.ApiUrl = $"https://{host}/")
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(Json(SearchNipJson)));
        using var provider = services.BuildServiceProvider();
        using var listener = ListenToWhiteListInstruments(out var instruments, host);

        // Act
        await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert: on .NET AddHttpClient registers the default IMeterFactory; on .NET Framework it doesn't, so the shared meter (no scope)
        var scope = instruments.ShouldHaveSingleItem().Meter.Scope;
#if NET
        scope.ShouldBeSameAs(provider.GetRequiredService<IMeterFactory>());
#else
        scope.ShouldBeNull();
#endif
    }

    // The instrument of each measurement on an OpenUrzednik.MF.WhiteList meter, of one API host when given.
    private static MeterListener ListenToWhiteListInstruments(out List<Instrument> instruments, string? host = null)
    {
        var recorded = instruments = [];
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == WhiteListTelemetry.SourceName)
                    l.EnableMeasurementEvents(instrument);
            },
        };
        listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
        {
            var ownHost = host is null;
            foreach (var tag in tags)
                ownHost |= tag.Key == "server.address" && Equals(tag.Value, host);
            if (ownHost)
            {
                lock (recorded)
                    recorded.Add(instrument);
            }
        });
        listener.Start();
        return listener;
    }

    // The operations recorded on the OpenUrzednik.MF.WhiteList meter for one API host.
    private static MeterListener ListenToWhiteListMeter(string host, out List<string> operations)
    {
        var recorded = operations = [];
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == WhiteListTelemetry.SourceName && instrument.Name == "openurzednik.client.operation.duration")
                    l.EnableMeasurementEvents(instrument);
            },
        };
        listener.SetMeasurementEventCallback<double>((_, _, tags, _) =>
        {
            string? operation = null;
            var ownHost = false;
            foreach (var tag in tags)
            {
                if (tag.Key == "openurzednik.operation")
                    operation = tag.Value as string;
                else if (tag.Key == "server.address")
                    ownHost = Equals(tag.Value, host);
            }

            if (ownHost && operation is not null)
            {
                lock (recorded)
                    recorded.Add(operation);
            }
        });
        listener.Start();
        return listener;
    }
}
