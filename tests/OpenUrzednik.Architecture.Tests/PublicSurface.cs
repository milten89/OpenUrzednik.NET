using System.Reflection;

namespace OpenUrzednik.Architecture.Tests;

/// <summary>
/// What an app can see of the published packages, by reflection: public types, protected nested types of unsealed public types,
/// and their public and protected members. Compiler-generated types and members (names starting with <c>&lt;</c>: closures,
/// state machines, a record's <c>&lt;Clone&gt;$</c>) aren't part of it. Property and event accessors are reached through their
/// properties and events; operators are methods.
/// </summary>
internal static class PublicSurface
{
    private const BindingFlags Declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static IEnumerable<Type> Types()
        => SrcAssemblies.All.SelectMany(assembly => assembly.GetTypes()).Where(IsVisible).Where(type => !IsGenerated(type));

    public static IEnumerable<MethodInfo> Methods(Type type)
        => type.GetMethods(Declared).Where(method => !IsAccessor(method) && !IsGenerated(method) && IsVisible(method, type));

    /// <summary>
    /// The private methods that implement an interface explicitly: callable through the interface, so they follow the same
    /// rules as public methods.
    /// </summary>
    public static IEnumerable<MethodInfo> ExplicitImplementations(Type type)
        => type.IsInterface
            ? []
            : type.GetInterfaces().SelectMany(i => type.GetInterfaceMap(i).TargetMethods)
                .Where(method => method.IsPrivate && method.DeclaringType == type && method.Name.IndexOf('.') >= 0);

    public static IEnumerable<ConstructorInfo> Constructors(Type type)
        => type.GetConstructors(Declared).Where(constructor => IsVisible(constructor, type));

    public static IEnumerable<PropertyInfo> Properties(Type type)
        => type.GetProperties(Declared).Where(property =>
            property.GetMethod is { } getter && IsVisible(getter, type) || property.SetMethod is { } setter && IsVisible(setter, type));

    public static IEnumerable<EventInfo> Events(Type type)
        => type.GetEvents(Declared).Where(@event => @event.AddMethod is { } add && IsVisible(add, type));

    public static IEnumerable<FieldInfo> Fields(Type type)
        => type.GetFields(Declared).Where(field =>
            !IsGenerated(field) && (field.IsPublic || ((field.IsFamily || field.IsFamilyOrAssembly) && !type.IsSealed)));

    /// <summary>A method of a type that the async rules care about: <c>Task</c>, <c>ValueTask</c> or <c>IAsyncEnumerable</c>.</summary>
    public static bool ReturnsAsync(MethodInfo method) => IsAsyncType(method.ReturnType);

    public static bool IsAsyncType(Type type)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return definition.FullName is "System.Threading.Tasks.Task" or "System.Threading.Tasks.Task`1"
            or "System.Threading.Tasks.ValueTask" or "System.Threading.Tasks.ValueTask`1"
            or "System.Collections.Generic.IAsyncEnumerable`1";
    }

    public static string Describe(MethodBase method)
        => $"{method.DeclaringType!.FullName}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})";

    private static bool IsVisible(Type type)
        => type.IsNested
            ? IsVisible(type.DeclaringType!)
              && (type.IsNestedPublic || ((type.IsNestedFamily || type.IsNestedFamORAssem) && !type.DeclaringType!.IsSealed))
            : type.IsPublic;

    private static bool IsVisible(MethodBase method, Type type)
        => method.IsPublic || ((method.IsFamily || method.IsFamilyOrAssembly) && !type.IsSealed);

    // Property and event accessors are special names; operators (op_…) are too, but they're methods an app calls.
    private static bool IsAccessor(MethodInfo method) => method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal);

    private static bool IsGenerated(MemberInfo member) => member.Name.StartsWith("<", StringComparison.Ordinal);
}
