using System.Reflection;

using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Extensions;
using ArchUnitNET.Loader;

using Shouldly;

using ArchUnitArchitecture = ArchUnitNET.Domain.Architecture;

namespace OpenUrzednik.Architecture.Tests;

/// <summary>
/// The published packages (<c>OpenUrzednik.Packages.slnf</c>), one type each; a new package adds its own
/// (<see cref="SrcAssembliesTest"/> fails until it does).
/// </summary>
internal static class SrcAssemblies
{
    public static readonly Assembly[] All =
    [
        typeof(Core.Errors.OpenUrzednikError).Assembly,
        typeof(Diagnostics.ActivityTraceSource).Assembly,
        typeof(Extensions.Logging.OpenUrzednikLogger).Assembly,
        typeof(MF.WhiteList.IWhiteListErrorCode).Assembly,
        typeof(Nbp.NbpGoldPriceClient).Assembly,
        typeof(global::Microsoft.Extensions.DependencyInjection.NbpServiceCollectionExtensions).Assembly,
        typeof(Transport.Http.HttpNotFoundError).Assembly,
    ];
}

/// <summary>
/// ArchUnitNET's model of <see cref="SrcAssemblies.All"/>, read from their files (the netstandard2.0 build on net472). Built on
/// first use, in its own class, so a loader failure fails only the ArchUnitNET rules.
/// </summary>
internal static class ArchUnitModel
{
    private static readonly Lazy<ArchUnitArchitecture> Model = new(() => new ArchLoader().LoadAssemblies(SrcAssemblies.All).Build());

    /// <summary>Fails with ArchUnitNET's list of violations when <paramref name="rule"/> doesn't hold.</summary>
    public static void ShouldHold(this IArchRule rule)
    {
        if (!rule.HasNoViolations(Model.Value))
            rule.Evaluate(Model.Value).ToErrorMessage().ShouldBeNull("the rule has violations");
    }
}
