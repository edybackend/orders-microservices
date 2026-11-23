using System.Text.Json;

namespace Ecommerce.Messaging;

internal static class MessagingJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
