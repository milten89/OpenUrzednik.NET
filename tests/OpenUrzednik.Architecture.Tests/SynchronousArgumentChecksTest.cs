using System.Reflection;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// ADR-0002: in a public method that returns a task, a null argument and an undefined enum value throw synchronously, before the
// task exists. An async method can't do that (its checks run inside the task), so such a method isn't async: it checks its
// arguments and returns the task of a private ...CoreAsync method.
public class SynchronousArgumentChecksTest
{
    private static readonly string[] StateMachineAttributes =
    [
        "System.Runtime.CompilerServices.AsyncStateMachineAttribute",
        "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute",
    ];

    [Fact]
    public void PublicAsyncMethod_WithReferenceOrEnumParameter_IsNotAnAsyncStateMachine()
    {
        // Act
        var violations = PublicSurface.Types()
            .SelectMany(PublicSurface.Methods)
            .Concat(PublicSurface.HiddenInterfaceImplementations())
            .Where(method => !method.IsAbstract && PublicSurface.ReturnsAsync(method))
            .Where(method => method.GetParameters().Any(p => NeedsCheck(p.ParameterType)))
            .Where(method => method.GetCustomAttributesData().Any(a => StateMachineAttributes.Contains(a.AttributeType.FullName)))
            .Select(PublicSurface.Describe)
            .ToList();

        // Assert
        violations.ShouldBeEmpty();
    }

    // A reference type can be null and an enum can be undefined; Nullable<enum> too. A generic parameter can be null unless it's
    // constrained to a non-nullable value type (struct, unmanaged), and can be an undefined value when it's constrained to Enum.
    private static bool NeedsCheck(Type type)
    {
        if (type.IsByRef)
            type = type.GetElementType()!;
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsGenericParameter)
        {
            return (type.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0
                   || type.GetGenericParameterConstraints().Contains(typeof(Enum));
        }
        return !type.IsValueType || type.IsEnum;
    }
}
