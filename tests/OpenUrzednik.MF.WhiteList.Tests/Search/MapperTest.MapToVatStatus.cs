using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Search;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

public sealed partial class MapperTest
{
    [Theory]
    [InlineData("Czynny", VatStatus.Active)]
    [InlineData("Zwolniony", VatStatus.Exempt)]
    [InlineData("Niezarejestrowany", VatStatus.NotRegistered)]
    [InlineData(null, null)]
    public void MapToVatStatus_KnownValue_ReturnsTheStatus(string? statusVat, VatStatus? expected)
    {
        // Act
        var status = Mapper.MapToVatStatus(statusVat);

        // Assert
        status.ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("czynny")] // the contract's values are case-sensitive
    [InlineData("Active")]
    public void MapToVatStatus_UnknownValue_ThrowsWithoutTheValue(string statusVat)
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatus(statusVat))
            .Message.ShouldBe("MF White List API response has an unknown 'statusVat'.");
    }
}
