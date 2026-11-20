using System.Diagnostics.Metrics;

using Microsoft.Extensions.DependencyInjection;

using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.DependencyInjection.Tests;

// ADR-0025: the DI registrations pass MeterMetrics, so their clients record on the shared OpenUrzednik.Nbp meter on every
// target, .NET Framework included. Other tests record on that meter at the same time, so each test uses its own API host.
public sealed partial class NbpServiceCollectionExtensionsTest
{
    [Fact]
    public async Task AddOpenUrzednikNbp_Client_RecordsOnTheNbpMeter()
    {
        // Arrange
        var host = $"{Guid.NewGuid():N}.example.com";
        var services = new ServiceCollection();
        services.AddOpenUrzednikNbp(options => options.ApiUrl = $"https://{host}/api/")
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(Json(GoldPriceJson)));
        using var provider = services.BuildServiceProvider();
        using var listener = ListenToNbpMeter(host, out var operations);

        // Act
        await provider.GetRequiredService<INbpGoldPriceClient>().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        operations.ShouldBe(["nbp.gold.latest"]);
    }

    [Fact]
    public async Task AddKeyedOpenUrzednikNbp_Client_RecordsOnTheNbpMeter()
    {
        // Arrange
        var host = $"{Guid.NewGuid():N}.example.com";
        var services = new ServiceCollection();
        services.AddKeyedOpenUrzednikNbp("gateway", options => options.ApiUrl = $"https://{host}/api/")
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(Json(GoldPriceJson)));
        using var provider = services.BuildServiceProvider();
        using var listener = ListenToNbpMeter(host, out var operations);

        // Act
        await provider.GetRequiredKeyedService<INbpGoldPriceClient>("gateway").GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        operations.ShouldBe(["nbp.gold.latest"]);
    }

    // The operations recorded on the OpenUrzednik.Nbp meter for one API host.
    private static MeterListener ListenToNbpMeter(string host, out List<string> operations)
    {
        var recorded = operations = [];
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == NbpTelemetry.SourceName && instrument.Name == "openurzednik.client.operation.duration")
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
