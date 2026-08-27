using System.Text.Json;

namespace OSDC.Drilling.Field.ModelShared;

public partial class Client
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        // EarthCartographicProjection uses camel-case JSON while EarthGeodesy
        // intentionally retains its established Pascal-case contract. Shared
        // dependency clients must accept both without changing either service.
        settings.PropertyNameCaseInsensitive = true;
    }
}
