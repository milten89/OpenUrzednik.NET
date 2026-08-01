using System.Text.Json.Serialization;

using OpenUrzednik.Nbp.Dto;

namespace OpenUrzednik.Nbp.Common;

[JsonSerializable(typeof(GoldPriceDto[]))]
[JsonSerializable(typeof(ExchangeRateTableDto[]))]
[JsonSerializable(typeof(BuySellExchangeRateTableDto[]))]
[JsonSerializable(typeof(CurrencyExchangeRatesDto))]
[JsonSerializable(typeof(CountryExchangeRatesDto))]
[JsonSerializable(typeof(BuySellCurrencyExchangeRatesDto))]
internal sealed partial class NbpJsonContext : JsonSerializerContext;
