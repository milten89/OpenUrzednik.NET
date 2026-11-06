using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using OpenUrzednik.Nbp.Table;

namespace OpenUrzednik.Nbp.Validation;

/// <summary>
/// Usage errors the clients throw synchronously, before a task is returned (ADR-0002): a value that isn't a defined enum member.
/// A value of the right type that fails a format or range check is a returned <c>ValidationError</c> instead.
/// </summary>
internal static class ArgumentGuard
{
    internal static void ThrowIfUndefined(TableType table, [CallerArgumentExpression(nameof(table))] string? paramName = null)
    {
        if (!Enum.IsDefined(table))
            ThrowUndefined(table, paramName);
    }

    [DoesNotReturn]
    private static void ThrowUndefined(TableType table, string? paramName)
        => throw new ArgumentOutOfRangeException(paramName, table, $"The value isn't defined by {nameof(TableType)}.");
}
