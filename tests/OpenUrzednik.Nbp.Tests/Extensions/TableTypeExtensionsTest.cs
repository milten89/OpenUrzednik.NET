using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Table;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Extensions;

public class TableTypeExtensionsTest
{
    [Theory]
    [InlineData(TableType.A, "A")]
    [InlineData(TableType.B, "B")]
    public void ToTagValue_DefinedTable_ReturnsItsLetter(TableType table, string expected)
    {
        // Act
        var result = table.ToTagValue();

        // Assert
        result.ShouldBe(expected);
    }
}
