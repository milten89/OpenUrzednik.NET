using System.Reflection;

namespace OpenUrzednik.Architecture.Tests;

/// <summary>
/// What an app can see of the published packages, by reflection: public types, protected nested types of unsealed public types,
/// and their public and protected members. Left out: compiler-generated types and members (names starting with <c>&lt;</c>:
/// closures, state machines, a record's <c>&lt;Clone&gt;$</c>), the runtime-implemented methods of a delegate (<c>Invoke</c>,
/// <c>BeginInvoke</c>, <c>EndInvoke</c>), and property and event accessors, which are reached through their properties and
/// events. Operators are methods.
/// </summary>
internal static class PublicSurface
{
    private const BindingFlags Declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static IEnumerable<Type> Types()
        => SrcAssemblies.All.SelectMany(assembly => assembly.GetTypes()).Where(IsVisible).Where(type => !IsGenerated(type));

    public static IEnumerable<MethodInfo> Methods(Type type)
        => type.GetMethods(Declared).Where(method => IsOwnMethod(method) && IsVisible(method, type));

    /// <summary>
    /// The private methods that implement a public interface explicitly: an app calls them through the interface, so they follow
    /// the same rules as public methods. Internal interfaces aren't public API.
    /// </summary>
    public static IEnumerable<MethodInfo> ExplicitImplementations(Type type)
        => type.IsInterface
            ? []
            : type.GetInterfaces().Where(i => i.IsVisible).SelectMany(i => type.GetInterfaceMap(i).TargetMethods)
                .Where(method => method.IsPrivate && method.DeclaringType == type && method.Name.IndexOf('.') >= 0);

    /// <summary>The interfaces of <paramref name="type"/> an app can see.</summary>
    public static IEnumerable<Type> VisibleInterfaces(Type type) => type.GetInterfaces().Where(i => i.IsVisible);

    public static IEnumerable<ConstructorInfo> Constructors(Type type)
        => type.GetConstructors(Declared).Where(constructor => IsVisible(constructor, type));

    public static IEnumerable<PropertyInfo> Properties(Type type)
        => type.GetProperties(Declared).Where(property => IsVisibleAccessor(property.GetMethod, type) || IsVisibleAccessor(property.SetMethod, type));

    public static IEnumerable<EventInfo> Events(Type type)
        => type.GetEvents(Declared).Where(@event => IsVisibleAccessor(@event.AddMethod, type));

    public static IEnumerable<FieldInfo> Fields(Type type)
        => type.GetFields(Declared).Where(field => !IsGenerated(field) && IsVisible(field, type));

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
    {
        if (!type.IsNested)
            return type.IsPublic;
        return IsVisible(type.DeclaringType!) && (type.IsNestedPublic || IsProtectedInUnsealed(type));
    }

    private static bool IsProtectedInUnsealed(Type nested)
        => (nested.IsNestedFamily || nested.IsNestedFamORAssem) && !nested.DeclaringType!.IsSealed;

    private static bool IsVisible(MethodBase method, Type type)
        => method.IsPublic || ((method.IsFamily || method.IsFamilyOrAssembly) && !type.IsSealed);

    private static bool IsVisible(FieldInfo field, Type type)
        => field.IsPublic || ((field.IsFamily || field.IsFamilyOrAssembly) && !type.IsSealed);

    private static bool IsVisibleAccessor(MethodInfo? accessor, Type type) => accessor is not null && IsVisible(accessor, type);

    // Not an accessor (special names other than the op_ operators), not compiler-generated, not a delegate's runtime method.
    private static bool IsOwnMethod(MethodInfo method)
    {
        if (method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal))
            return false;
        return !IsGenerated(method) && (method.MethodImplementationFlags & MethodImplAttributes.Runtime) == 0;
    }

    private static bool IsGenerated(MemberInfo member) => member.Name.StartsWith("<", StringComparison.Ordinal);
}
