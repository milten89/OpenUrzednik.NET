using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// ADR-0025: the library's measurements are internal classes, with their name, unit and tags set where they are measured, so
// no public list of measurement types or names grows with each metric; apps see them only through the measurement interfaces.
public class MeasurementsTest
{
    [Fact]
    public void PublicTypes_DontImplementAMeasurement()
    {
        // Act
        var violations = PublicSurface.Types()
            .Where(type => !type.IsInterface && typeof(IOpenUrzednikMeasurement).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .ToList();

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Assemblies_HaveAnInternalMeasurement()
    {
        // Act
        var measurements = SrcAssemblies.All
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsInterface && typeof(IOpenUrzednikMeasurement).IsAssignableFrom(type))
            .ToList();

        // Assert: the first test would pass trivially if the discovery broke
        measurements.ShouldNotBeEmpty("no measurement class found: the discovery broke");
    }
}
