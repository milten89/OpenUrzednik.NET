using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class MapperTest
{
    [Theory]
    [InlineData(MidTableType.A, NbpTable.A)]
    [InlineData(MidTableType.B, NbpTable.B)]
    public void MapToNbpTable_DefinedValue_ReturnsMappedNbpTable(MidTableType input, NbpTable expected)
    {
        // Act
        var result = Mapper.MapToNbpTable(input);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData((MidTableType)99)]
    [InlineData((MidTableType)(-1))]
    public void MapToNbpTable_UndefinedValue_ThrowsArgumentException(MidTableType input)
    {
        // Act && Assert
        Should.Throw<ArgumentException>(() => Mapper.MapToNbpTable(input))
            .ParamName.ShouldBe("table");
    }
}