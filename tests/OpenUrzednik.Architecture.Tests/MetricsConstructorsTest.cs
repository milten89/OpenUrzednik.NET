using System.Reflection;

using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// ADR-0025: a client that takes a trace source also takes metrics, in a constructor of its own; the constructor it replaced is
// obsolete until 2.0 and keeps no default values, so a call with only some of the optional arguments isn't ambiguous (CS0121).
public class MetricsConstructorsTest
{
    [Fact]
    public void PublicTypes_TakingATraceSource_HaveAConstructorTakingMetrics()
    {
        // Arrange
        var clients = PublicSurface.Types()
            .Where(type => PublicSurface.Constructors(type).Any(constructor => Takes<IOpenUrzednikTraceSource>(constructor)))
            .ToList();

        // Act
        var violations = clients
            .Where(type => !PublicSurface.Constructors(type).Any(constructor => constructor.GetCustomAttribute<ObsoleteAttribute>() is null
                                                                                && Takes<IOpenUrzednikTraceSource>(constructor)
                                                                                && Takes<IOpenUrzednikMetrics>(constructor)))
            .Select(type => type.FullName)
            .ToList();

        // Assert
        clients.ShouldNotBeEmpty("no client constructor with a trace source found: the discovery broke");
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void ObsoleteClientConstructors_HaveNoOptionalParameters()
    {
        // Arrange: an obsolete client constructor and its replacement share their leading optional parameters, so with defaults
        // on both a call passing only some of them would be ambiguous (CS0121); OptionalParametersTest skips obsolete members
        var obsolete = PublicSurface.Types()
            .SelectMany(PublicSurface.Constructors)
            .Where(constructor => Takes<IOpenUrzednikTraceSource>(constructor) && constructor.GetCustomAttribute<ObsoleteAttribute>() is not null)
            .ToList();

        // Act
        var violations = obsolete
            .Where(constructor => constructor.GetParameters().Any(parameter => parameter.IsOptional))
            .Select(constructor => constructor.DeclaringType!.FullName)
            .ToList();

        // Assert
        obsolete.ShouldNotBeEmpty("no obsolete client constructor found: the discovery broke");
        violations.ShouldBeEmpty();
    }

    private static bool Takes<T>(MethodBase member) => member.GetParameters().Any(parameter => parameter.ParameterType == typeof(T));
}
