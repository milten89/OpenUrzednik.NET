using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// One entry of a batch search: the identifier and either the subjects the list has for it on the requested date, or why it has none.
/// </summary>
/// <remarks>
/// <para>
/// <paramref name="Error"/> is a <see cref="WhiteListNotFoundError"/> with the batch's proof when no subject on the list has the
/// identifier (it derives from <see cref="NotFoundError"/>), or a <see cref="WhiteListQueryRejectedError"/> when the API rejected the
/// identifier (e.g. <c>WL-115</c>, an invalid NIP; it derives from <see cref="BadRequestError"/>). The client validates every identifier
/// before it sends the batch, so a rejected entry is rare. A failed entry doesn't fail the call.
/// </para>
/// <para>
/// Two entries are equal when their identifiers, subjects (item by item) and errors are; two errors are equal when they have the same
/// type, <see cref="OpenUrzednikError.Message"/> and <see cref="OpenUrzednikError.Metadata"/> (which holds the error's details, such
/// as the proof or the API's code; values compared with <see cref="object.Equals(object, object)"/>, so an array value only by reference,
/// though the errors the client returns hold none), not only when they are the same instance.
/// </para>
/// </remarks>
/// <param name="Identifier">The identifier as the API echoes it: the plain digits the client sent (separators and the <c>PL</c> prefix removed).</param>
/// <param name="Subjects">
/// The subjects with the identifier on the requested date: one for a NIP or REGON, one or more for an account number; empty when
/// <paramref name="Error"/> is set.
/// </param>
/// <param name="Error">Why the entry has no subjects, or <see langword="null"/> when it has them.</param>
public sealed record BatchSearchEntry(string Identifier, IReadOnlyList<Subject> Subjects, OpenUrzednikError? Error)
{
    /// <summary>Gets a value indicating whether the entry has its subjects (<see cref="Error"/> is <see langword="null"/>).</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Compares all members, <c>Subjects</c> item by item, and <c>Error</c> by type, message and metadata.</summary>
    /// <param name="other">Record to compare with.</param>
    /// <returns><see langword="true"/> if the records are equal.</returns>
    public bool Equals(BatchSearchEntry? other)
        => other is not null &&
           Identifier == other.Identifier &&
           Subjects.SequenceEqual(other.Subjects) &&
           ErrorEquals(Error, other.Error);

    /// <inheritdoc/>
    public override int GetHashCode()
        => (Identifier, Subjects.Count, Error?.Message).GetHashCode();

    // The same type, message and metadata entries; the metadata holds every detail of the provider's errors (code, proof).
    private static bool ErrorEquals(OpenUrzednikError? error, OpenUrzednikError? other)
    {
        if (error is null || other is null)
            return error is null && other is null;

        if (error.GetType() != other.GetType() || error.Message != other.Message)
            return false;

        // The counts in locals, so that mutation testing can compile the mutants of the comparison (ADR-0020).
        var metadata = error.Metadata;
        var otherMetadata = other.Metadata;
        var count = metadata.Count;
        var otherCount = otherMetadata.Count;
        if (count != otherCount)
            return false;

        foreach (var entry in metadata)
        {
            if (!otherMetadata.TryGetValue(entry.Key, out var value) || !Equals(entry.Value, value))
                return false;
        }

        return true;
    }
}
