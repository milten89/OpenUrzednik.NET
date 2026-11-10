using System.Text.Json.Serialization;

using OpenUrzednik.MF.WhiteList.Dto;

namespace OpenUrzednik.MF.WhiteList.Common;

[JsonSerializable(typeof(ErrorDto))]
internal sealed partial class WhiteListJsonContext : JsonSerializerContext;
