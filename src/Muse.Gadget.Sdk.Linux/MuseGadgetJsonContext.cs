using System.Text.Json.Serialization;

namespace Muse.Gadget.Sdk.Linux;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MessageRequest))]
[JsonSerializable(typeof(ServiceResponse))]
internal sealed partial class MuseGadgetJsonContext : JsonSerializerContext
{
}
