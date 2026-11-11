using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UGeekStore.Services;

/// <summary>
/// Sends "new product" announcements to customers.
/// WhatsApp: if Messaging:WhatsApp:Token and PhoneNumberId (Meta Cloud API) are set, sends an image message;
/// otherwise the UI offers wa.me click-to-chat links. SMS: posts to a generic HTTP gateway if Messaging:Sms:Url is set.
/// </summary>
public class MessagingService
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _cfg;
    private readonly ILogger<MessagingService> _log;

    public MessagingService(IHttpClientFactory http, IConfiguration cfg, ILogger<MessagingService> log)
    {
        _http = http; _cfg = cfg; _log = log;
    }

    public bool WhatsAppApiConfigured => !string.IsNullOrEmpty(_cfg["Messaging:WhatsApp:Token"]) && !string.IsNullOrEmpty(_cfg["Messaging:WhatsApp:PhoneNumberId"]);
    public bool SmsConfigured => !string.IsNullOrEmpty(_cfg["Messaging:Sms:Url"]);

    public static string WhatsAppLink(string phone, string text) =>
        $"https://wa.me/{phone}?text={Uri.EscapeDataString(text)}";

    public async Task<bool> SendWhatsAppAsync(string phone, string caption, string? imageUrl)
    {
        if (!WhatsAppApiConfigured) return false;
        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cfg["Messaging:WhatsApp:Token"]);
        object payload = imageUrl != null
            ? new { messaging_product = "whatsapp", to = phone, type = "image", image = new { link = imageUrl, caption } }
            : new { messaging_product = "whatsapp", to = phone, type = "text", text = new { body = caption } };
        var url = $"https://graph.facebook.com/v20.0/{_cfg["Messaging:WhatsApp:PhoneNumberId"]}/messages";
        return await PostAsync(client, url, payload);
    }

    public async Task<bool> SendSmsAsync(string phone, string text)
    {
        if (!SmsConfigured) return false;
        var client = _http.CreateClient();
        var key = _cfg["Messaging:Sms:ApiKey"];
        if (!string.IsNullOrEmpty(key)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        return await PostAsync(client, _cfg["Messaging:Sms:Url"]!, new { to = phone, from = _cfg["Messaging:Sms:Sender"], text });
    }

    private async Task<bool> PostAsync(HttpClient client, string url, object payload)
    {
        try
        {
            var res = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            if (!res.IsSuccessStatusCode) _log.LogWarning("Messaging failed {Status}: {Body}", res.StatusCode, await res.Content.ReadAsStringAsync());
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Messaging failed");
            return false;
        }
    }
}
