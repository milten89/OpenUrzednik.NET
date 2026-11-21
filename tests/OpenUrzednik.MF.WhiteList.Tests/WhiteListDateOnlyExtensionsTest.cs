#if NET
using NSubstitute;

namespace OpenUrzednik.MF.WhiteList.Tests;

public partial class WhiteListDateOnlyExtensionsTest
{
    // The extensions pass midnight with DateTimeKind.Unspecified; DateTime equality alone ignores Kind.
    private static DateTime Midnight(int year, int month, int day)
        => Arg.Is<DateTime>(x => x == new DateTime(year, month, day) && x.Kind == DateTimeKind.Unspecified);
}
#endif
