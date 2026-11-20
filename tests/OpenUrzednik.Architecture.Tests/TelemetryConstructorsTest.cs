using System.Reflection;

using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// ADR-0025: a client takes its logger, trace source and metrics as one OpenUrzednikTelemetry. Until 2.0 the 1.0 constructor with
// a logger and a trace source stays, obsolete and with its default values, so 1.0 code still compiles; the new constructors are
// prefix overloads without default values, else a call with only some arguments would be ambiguous (CS0121).
public class TelemetryConstructorsTest
{
    private static readonly Type[] TelemetryKinds = [typeof(IOpenUrzednikLogger), typeof(IOpenUrzednikTraceSource), typeof(IOpenUrzednikMetrics)];

    // A client is a public type built from an HttpClient, so one that takes no telemetry at all is found too.
    private static List<Type> Clients() => PublicSurface.Types()
        .Where(type => PublicSurface.Constructors(type).Any(constructor => constructor.GetParameters().FirstOrDefault()?.ParameterType == typeof(HttpClient)))
        .ToList();

    [Fact]
    public void Clients_HaveAConstructorTakingOpenUrzednikTelemetry()
    {
        // Arrange
        var clients = Clients();

        // Act
        var violations = clients
            .Where(type => !PublicSurface.Constructors(type).Any(constructor => !IsObsolete(constructor) && Takes(constructor, typeof(OpenUrzednikTelemetry))))
            .Select(type => type.FullName)
            .ToList();

        // Assert
        clients.ShouldNotBeEmpty("no client constructor with telemetry found: the discovery broke");
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Clients_TakeTelemetryKindsOnlyInObsoleteConstructors()
    {
        // Act: a logger, trace source or metrics passed on its own instead of in an OpenUrzednikTelemetry
        var violations = Clients()
            .SelectMany(PublicSurface.Constructors)
            .Where(constructor => !IsObsolete(constructor) && TelemetryKinds.Any(kind => Takes(constructor, kind)))
            .Select(constructor => constructor.DeclaringType!.FullName)
            .ToList();

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Clients_WithAnObsoleteConstructor_HaveNoOtherOptionalParameters()
    {
        // Arrange: the obsolete constructor keeps the 1.0 default values; OptionalParametersTest skips obsolete members, but
        // overload resolution doesn't
        var clients = Clients().Where(type => PublicSurface.Constructors(type).Any(IsObsolete)).ToList();

        // Act
        var violations = clients
            .SelectMany(PublicSurface.Constructors)
            .Where(constructor => !IsObsolete(constructor) && constructor.GetParameters().Any(parameter => parameter.IsOptional))
            .Select(constructor => constructor.DeclaringType!.FullName)
            .ToList();

        // Assert: 2.0 removes the obsolete constructors, and this guard with them
        clients.ShouldNotBeEmpty("no client with an obsolete constructor found: the discovery broke");
        violations.ShouldBeEmpty();
    }

    private static bool IsObsolete(MemberInfo member) => member.GetCustomAttribute<ObsoleteAttribute>() is not null;

    private static bool Takes(MethodBase member, Type type) => member.GetParameters().Any(parameter => parameter.ParameterType == type);
}
