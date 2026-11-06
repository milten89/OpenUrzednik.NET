namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// Identifies a kind of log entry, like <c>EventId</c> in Microsoft.Extensions.Logging. Every entry a client writes has one.
/// </summary>
/// <remarks>
/// The ids and names are part of the telemetry contract (ADR-0003): an id is never renumbered or reused within a major version.
/// Providers list theirs in a public class, e.g. <c>NbpLogEvents</c>.
/// </remarks>
public readonly struct OpenUrzednikEventId : IEquatable<OpenUrzednikEventId>
{
    /// <summary>Creates an event id.</summary>
    /// <param name="id">The numeric id.</param>
    /// <param name="name">The event name, e.g. <c>RequestTimedOut</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    public OpenUrzednikEventId(int id, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Id = id;
        Name = name;
    }

    /// <summary>Gets the numeric id.</summary>
    public int Id { get; }

    /// <summary>Gets the event name. <see langword="null"/> only for <see langword="default"/>.</summary>
    public string? Name { get; }

    /// <summary>Returns whether both ids have the same <see cref="Id"/> and <see cref="Name"/>.</summary>
    /// <param name="left">The first id.</param>
    /// <param name="right">The second id.</param>
    public static bool operator ==(OpenUrzednikEventId left, OpenUrzednikEventId right) => left.Equals(right);

    /// <summary>Returns whether the ids differ in <see cref="Id"/> or <see cref="Name"/>.</summary>
    /// <param name="left">The first id.</param>
    /// <param name="right">The second id.</param>
    public static bool operator !=(OpenUrzednikEventId left, OpenUrzednikEventId right) => !left.Equals(right);

    /// <inheritdoc/>
    public bool Equals(OpenUrzednikEventId other) => Id == other.Id && string.Equals(Name, other.Name, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is OpenUrzednikEventId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Id;

    /// <summary>Returns the name, or the id when there is no name.</summary>
    public override string ToString() => Name ?? Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
