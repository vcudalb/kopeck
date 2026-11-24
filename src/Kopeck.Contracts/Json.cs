using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kopeck.Contracts;

/// <summary>Shared JSON settings so the API and the mobile app serialize identically.</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = Create();

    public static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }
}
