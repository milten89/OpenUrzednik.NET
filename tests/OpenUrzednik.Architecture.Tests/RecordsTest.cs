using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// Code conventions: a record's generated Equals compares a list by reference, so a public record holding a collection declares its
// own Equals and GetHashCode (Currency/CurrencyExchangeRates.cs). The generated ones carry [CompilerGenerated]. A record, class or
// struct, is recognised by the PrintMembers(StringBuilder) the compiler gives it. The generated Equals compares every instance
// field, so every field counts, whatever its visibility (a property's backing field included).
public class RecordsTest
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    public void PublicRecord_WithCollectionField_DeclaresItsOwnEquality()
    {
        // Arrange
        var records = PublicSurface.Types()
            .Where(type => type.GetMethod("PrintMembers", Declared | BindingFlags.NonPublic, null, [typeof(System.Text.StringBuilder)], null) is not null)
            .Where(type => type.GetFields(Declared | BindingFlags.NonPublic).Any(f => f.FieldType != typeof(string)
                                                                                        && typeof(IEnumerable).IsAssignableFrom(f.FieldType)))
            .ToList();

        // Act
        var violations = records
            .Where(type => !IsWritten(type.GetMethod("Equals", Declared, null, [type], null))
                           || !IsWritten(type.GetMethod("GetHashCode", Declared, null, Type.EmptyTypes, null)))
            .Select(type => type.FullName)
            .ToList();

        // Assert
        records.ShouldNotBeEmpty("no public record with a collection found: the record detection broke");
        violations.ShouldBeEmpty();
    }

    private static bool IsWritten(MethodInfo? method) => method is not null && !method.IsDefined(typeof(CompilerGeneratedAttribute), false);
}
