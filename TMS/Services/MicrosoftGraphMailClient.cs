using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace TMS.Services;

public sealed class MicrosoftGraphMailClient(
    IHttpClientFactory httpClientFactory,
    IOptions<TicketMailOptions> options)
{
    private static readonly string[] Scopes = ["https://graph.microsoft.com/.default"];
    private readonly TicketMailOptions _options = options.Value;
    private TokenCredential? _credential;

    public string BuildInitialDeltaUrl(DateTime sinceUtc)
    {
        var mailbox = Uri.EscapeDataString(_options.MailboxAddress.Trim());
        var since = Uri.EscapeDataString(sinceUtc.ToUniversalTime().ToString("O"));
        // Delta pages intentionally omit message bodies. With a personal test mailbox we first
        // inspect metadata and download content only for messages that pass the subject filter.
        const string select = "id,internetMessageId,conversationId,subject,from,receivedDateTime,isDraft";
        return $"https://graph.microsoft.com/v1.0/users/{mailbox}/mailFolders/inbox/messages/delta" +
            $"?changeType=created&$select={select}&$filter=receivedDateTime%20ge%20{since}";
    }

    public async Task<GraphMailDeltaPage> GetDeltaPageAsync(string url, CancellationToken cancellationToken)
        => await GetAsync<GraphMailDeltaPage>(url, cancellationToken) ?? new GraphMailDeltaPage();

    public async Task<GraphMailMessage?> GetMessageAsync(string messageId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(messageId)) return null;
        var mailbox = Uri.EscapeDataString(_options.MailboxAddress.Trim());
        var id = Uri.EscapeDataString(messageId);
        const string select = "id,internetMessageId,conversationId,subject,body,bodyPreview,from,receivedDateTime,isDraft";
        return await GetAsync<GraphMailMessage>(
            $"https://graph.microsoft.com/v1.0/users/{mailbox}/messages/{id}?$select={select}", cancellationToken);
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("Microsoft 365 posta bağlantısı henüz yapılandırılmadı.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, "graph.microsoft.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Microsoft Graph senkronizasyon adresi geçersiz.");

        _credential ??= new ClientSecretCredential(
            _options.TenantId.Trim(), _options.ClientId.Trim(), _options.ClientSecret);
        var token = await _credential.GetTokenAsync(new TokenRequestContext(Scopes), cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        request.Headers.TryAddWithoutValidation("Prefer", $"odata.maxpagesize={Math.Clamp(_options.PageSize, 1, 200)}");
        request.Headers.TryAddWithoutValidation("Prefer", "IdType=\"ImmutableId\"");
        request.Headers.TryAddWithoutValidation("Prefer", "outlook.body-content-type=\"text\"");

        using var response = await httpClientFactory.CreateClient("MicrosoftGraph")
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new MicrosoftGraphMailException(response.StatusCode,
                $"Microsoft Graph posta isteği başarısız oldu ({(int)response.StatusCode}). " +
                Truncate(responseBody, 1000));
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}

public sealed class MicrosoftGraphMailException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed class GraphMailDeltaPage
{
    [JsonPropertyName("value")] public List<GraphMailMessage> Messages { get; set; } = [];
    [JsonPropertyName("@odata.nextLink")] public string? NextLink { get; set; }
    [JsonPropertyName("@odata.deltaLink")] public string? DeltaLink { get; set; }
}

public sealed class GraphMailMessage
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("internetMessageId")] public string? InternetMessageId { get; set; }
    [JsonPropertyName("conversationId")] public string? ConversationId { get; set; }
    [JsonPropertyName("subject")] public string? Subject { get; set; }
    [JsonPropertyName("body")] public GraphMailBody? Body { get; set; }
    [JsonPropertyName("bodyPreview")] public string? BodyPreview { get; set; }
    [JsonPropertyName("from")] public GraphMailRecipient? From { get; set; }
    [JsonPropertyName("receivedDateTime")] public DateTimeOffset? ReceivedDateTime { get; set; }
    [JsonPropertyName("isDraft")] public bool IsDraft { get; set; }
    [JsonPropertyName("@removed")] public JsonElement? Removed { get; set; }
}

public sealed class GraphMailBody
{
    [JsonPropertyName("contentType")] public string? ContentType { get; set; }
    [JsonPropertyName("content")] public string? Content { get; set; }
}

public sealed class GraphMailRecipient
{
    [JsonPropertyName("emailAddress")] public GraphMailAddress? EmailAddress { get; set; }
}

public sealed class GraphMailAddress
{
    [JsonPropertyName("address")] public string? Address { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
}
