using System.Reflection;
using System.Runtime.CompilerServices;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// ADR-0005: every build has the same public signatures, so public dates are DateTime; DateOnly overloads exist only as .NET-only
// extension methods (the *DateOnlyExtensions classes). The netstandard2.0 build can't use DateOnly, so on net472 (where the tests
// alias DateOnly to DateTime) there is nothing to find and the type is null.
public class DateOnlyTest
{
#if NET
    private static readonly Type? DateOnlyType = typeof(DateOnly);
#else
    private static readonly Type? DateOnlyType = null;
#endif

    [Fact]
    public void PublicSignature_OutsideDateOnlyExtensions_NeverUsesDateOnly()
    {
        // Act
        var violations = PublicSurface.Types().SelectMany(Uses).ToList();

        // Assert
        violations.ShouldBeEmpty();
    }

    private static IEnumerable<string> Uses(Type type)
    {
        var name = type.FullName;
        var inherited = PublicSurface.VisibleInterfaces(type).Append(type.BaseType).OfType<Type>();
        var typeParameters = type.IsGenericTypeDefinition ? type.GetGenericArguments() : [];
        foreach (var used in inherited.Concat(typeParameters).Where(UsesDateOnly))
            yield return $"{name} : {used.Name}";
        if (IsDelegate(type) && Signature(type.GetMethod("Invoke")!).Any(UsesDateOnly))
            yield return $"{name}.Invoke";
        var extensionsExempt = IsDateOnlyExtensions(type);
        foreach (var method in PublicSurface.Methods(type).Cast<MethodBase>().Concat(PublicSurface.Constructors(type))
                     .Where(method => !(extensionsExempt && method.IsDefined(typeof(ExtensionAttribute), false)))
                     .Where(method => Signature(method).Any(UsesDateOnly)))
            yield return PublicSurface.Describe(method);
        foreach (var property in PublicSurface.Properties(type)
                     .Where(p => UsesDateOnly(p.PropertyType) || p.GetIndexParameters().Any(i => UsesDateOnly(i.ParameterType))))
            yield return $"{name}.{property.Name}";
        foreach (var @event in PublicSurface.Events(type).Where(e => e.EventHandlerType is { } handler && UsesDateOnly(handler)))
            yield return $"{name}.{@event.Name}";
        foreach (var field in PublicSurface.Fields(type).Where(f => UsesDateOnly(f.FieldType)))
            yield return $"{name}.{field.Name}";
    }

    // The return type, the parameters and a generic method's type parameters (whose constraints are part of the signature).
    private static IEnumerable<Type> Signature(MethodBase method)
    {
        if (method is MethodInfo info)
            yield return info.ReturnType;
        foreach (var parameter in method.GetParameters())
            yield return parameter.ParameterType;
        if (method.IsGenericMethodDefinition)
        {
            foreach (var typeParameter in method.GetGenericArguments())
                yield return typeParameter;
        }
    }

    // The exemption is for the .NET-only DateOnly overloads: the extension methods (classic, or a C# 14 extension block's) of a
    // static class named *DateOnlyExtensions. Anything else in such a class still counts.
    private static bool IsDateOnlyExtensions(Type type)
        => type.IsAbstract && type.IsSealed && type.Name.EndsWith("DateOnlyExtensions", StringComparison.Ordinal);

    private static bool IsDelegate(Type type) => type.IsSubclassOf(typeof(Delegate));

    private static bool UsesDateOnly(Type type) => DateOnlyType is not null && UsesDateOnly(type, []);

    // Through arrays, by-ref and pointer types, Nullable<T>, every other generic argument (Task<DateOnly>, IReadOnlyList<...>), the
    // constraints of a generic parameter (where T : IComparable<DateOnly>) and the signature of a delegate the packages declare
    // (an event's custom handler); `seen` stops at T : IComparable<T> and at a delegate that refers to itself.
    private static bool UsesDateOnly(Type type, HashSet<Type> seen)
    {
        if (type == DateOnlyType)
            return true;
        if (type.HasElementType)
            return UsesDateOnly(type.GetElementType()!, seen);
        if (type.IsGenericParameter)
            return seen.Add(type) && type.GetGenericParameterConstraints().Any(constraint => UsesDateOnly(constraint, seen));
        if (type.IsGenericType && type.GetGenericArguments().Any(argument => UsesDateOnly(argument, seen)))
            return true;
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return IsDelegate(definition) && SrcAssemblies.All.Contains(definition.Assembly) && seen.Add(definition)
               && Signature(definition.GetMethod("Invoke")!).Any(part => UsesDateOnly(part, seen));
    }
}
