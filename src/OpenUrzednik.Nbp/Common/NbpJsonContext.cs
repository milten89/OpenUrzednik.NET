using System.Text.Json.Serialization;

using OpenUrzednik.Nbp.Dto;

namespace OpenUrzednik.Nbp.Common;

[JsonSerializable(typeof(GoldPriceDto[]))]
internal sealed partial class NbpJsonContext : JsonSerializerContext;
