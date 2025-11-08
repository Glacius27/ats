using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AuthorizationService.Services;

public class KeycloakOptions
{
    public string BaseUrl { get; set; } = null!;
    public string Realm { get; set; } = null!;

    // любой из двух способов аутентификации (client creds ИЛИ username/password)
    public string? AdminClientId { get; set; }
    public string? AdminClientSecret { get; set; }
    public string? AdminUsername { get; set; }
    public string? AdminPassword { get; set; }
}

public class KeycloakAdminClient
{
    private readonly HttpClient _http;
    private readonly KeycloakOptions _opt;

    private string? _cachedToken;
    private DateTime _tokenExp = DateTime.MinValue;

    public KeycloakAdminClient(HttpClient http, IOptions<KeycloakOptions> opt)
    {
        _http = http;
        _opt = opt.Value;

        if (string.IsNullOrWhiteSpace(_opt.BaseUrl))
            throw new ArgumentException("Keycloak:BaseUrl is not configured");
        if (string.IsNullOrWhiteSpace(_opt.Realm))
            throw new ArgumentException("Keycloak:Realm is not configured");

        _http.BaseAddress = new Uri(_opt.BaseUrl.TrimEnd('/') + "/");
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExp)
            return _cachedToken!;

        var url = $"realms/{_opt.Realm}/protocol/openid-connect/token";
        var pairs = new List<KeyValuePair<string, string>>();

        if (!string.IsNullOrEmpty(_opt.AdminClientSecret))
        {
            // client_credentials
            pairs.Add(new("grant_type", "client_credentials"));
            pairs.Add(new("client_id", _opt.AdminClientId!));
            pairs.Add(new("client_secret", _opt.AdminClientSecret!));
        }
        else
        {
            // password
            pairs.Add(new("grant_type", "password"));
            pairs.Add(new("client_id", _opt.AdminClientId!));
            pairs.Add(new("username", _opt.AdminUsername!));
            pairs.Add(new("password", _opt.AdminPassword!));
        }

        using var content = new FormUrlEncodedContent(pairs);
        using var resp = await _http.PostAsync(url, content, ct);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        _cachedToken = doc.RootElement.GetProperty("access_token").GetString();
        var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExp = DateTime.UtcNow.AddSeconds(expiresIn - 30);

        return _cachedToken!;
    }

    private async Task WithAuthAsync(CancellationToken ct = default)
    {
        var token = await GetAdminTokenAsync(ct);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<string> CreateUserAsync(string username, string email, CancellationToken ct = default)
    {
        await WithAuthAsync(ct);

        var payload = new { username, email, enabled = true };
        var url = $"admin/realms/{_opt.Realm}/users";
        using var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync(url, body, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Keycloak CreateUser failed: {(int)resp.StatusCode} {err}");
        }

        var location = resp.Headers.Location ?? throw new InvalidOperationException("Keycloak did not return Location header");
        var id = location.Segments.Last().Trim('/');
        return id;
    }

    public async Task SetPasswordAsync(string userId, string password, CancellationToken ct = default)
    {
        await WithAuthAsync(ct);

        var payload = new { type = "password", value = password, temporary = false };
        var url = $"admin/realms/{_opt.Realm}/users/{userId}/reset-password";
        using var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var resp = await _http.PutAsync(url, body, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Keycloak SetPassword failed: {(int)resp.StatusCode} {err}");
        }
    }

    public async Task<bool> DeleteUserAsync(string keycloakUserId, CancellationToken ct = default)
    {
        await WithAuthAsync(ct);
        var url = $"admin/realms/{_opt.Realm}/users/{keycloakUserId}";
        using var resp = await _http.DeleteAsync(url, ct);
        return resp.IsSuccessStatusCode;
    }
}