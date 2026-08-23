using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class MapperTest
{
    [Theory]
    [InlineData(TableType.A, NbpTable.A)]
    [InlineData(TableType.B, NbpTable.B)]
    public void MapToNbpTable_DefinedValue_ReturnsMappedNbpTable(TableType input, NbpTable expected)
    {
        // Act
        var result = Mapper.MapToNbpTable(input);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData((TableType)99)]
    [InlineData((TableType)(-1))]
    public void MapToNbpTable_UndefinedValue_ThrowsArgumentException(TableType input)
    {
        // Act && Assert
        Should.Throw<ArgumentException>(() => Mapper.MapToNbpTable(input))
            .ParamName.ShouldBe("table");
    }
}
