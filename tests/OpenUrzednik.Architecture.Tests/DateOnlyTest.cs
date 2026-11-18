using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// ADR-0005: every build has the same public signatures, so public dates are DateTime; DateOnly overloads exist only as .NET-only
// extension methods (the *DateOnlyExtensions classes). Compared by full name: on net472 the tests alias DateOnly to DateTime.
public class DateOnlyTest
{
    [Fact]
    public void PublicSignature_OutsideDateOnlyExtensions_NeverUsesDateOnly()
    {
        // Act
        var violations = PublicSurface.Types()
            .Where(type => !type.Name.EndsWith("DateOnlyExtensions", StringComparison.Ordinal))
            .SelectMany(Uses)
            .ToList();

        // Assert
        violations.ShouldBeEmpty();
    }

    private static IEnumerable<string> Uses(Type type)
    {
        var name = type.FullName;
        foreach (var inherited in type.GetInterfaces().Append(type.BaseType).OfType<Type>().Where(UsesDateOnly))
            yield return $"{name} : {inherited.Name}";
        foreach (var method in PublicSurface.Methods(type).Cast<System.Reflection.MethodBase>().Concat(PublicSurface.Constructors(type)))
        {
            var returnType = (method as System.Reflection.MethodInfo)?.ReturnType;
            if ((returnType is not null && UsesDateOnly(returnType)) || method.GetParameters().Any(p => UsesDateOnly(p.ParameterType)))
                yield return PublicSurface.Describe(method);
        }
        foreach (var property in PublicSurface.Properties(type)
                     .Where(p => UsesDateOnly(p.PropertyType) || p.GetIndexParameters().Any(i => UsesDateOnly(i.ParameterType))))
            yield return $"{name}.{property.Name}";
        foreach (var @event in PublicSurface.Events(type).Where(e => e.EventHandlerType is { } handler && UsesDateOnly(handler)))
            yield return $"{name}.{@event.Name}";
        foreach (var field in PublicSurface.Fields(type).Where(f => UsesDateOnly(f.FieldType)))
            yield return $"{name}.{field.Name}";
    }

    // Through arrays, by-ref and pointer types, Nullable<T> and every other generic argument (Task<DateOnly>, IReadOnlyList<...>).
    private static bool UsesDateOnly(Type type)
        => type.FullName == "System.DateOnly"
           || (type.HasElementType && UsesDateOnly(type.GetElementType()!))
           || (type.IsGenericType && type.GetGenericArguments().Any(UsesDateOnly));
}
