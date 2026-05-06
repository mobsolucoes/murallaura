using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using HashtagWall.Api.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HashtagWall.Api.Services;

public sealed class MetaOAuthFlow
{
    private readonly MetaOAuthOptions _opt;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<MetaOAuthFlow> _logger;

    private static readonly TimeSpan PendingTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ResultTtl = TimeSpan.FromMinutes(5);

    private const string PendingPrefix = "meta_oauth_pending:";
    private const string ResultPrefix = "meta_oauth_result:";

    public MetaOAuthFlow(
        IOptions<MetaOAuthOptions> options,
        IMemoryCache cache,
        IHttpClientFactory httpFactory,
        ILogger<MetaOAuthFlow> logger)
    {
        _opt = options.Value;
        _cache = cache;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public bool IsConfigured =>
        _opt.Enabled &&
        !string.IsNullOrWhiteSpace(_opt.AppId) &&
        !string.IsNullOrWhiteSpace(_opt.AppSecret) &&
        !string.IsNullOrWhiteSpace(_opt.RedirectUri) &&
        !string.IsNullOrWhiteSpace(_opt.FrontendBaseUrl);

    /// <summary>Inicia fluxo: armazena estado e devolve URL do Facebook Login.</summary>
    public string BeginAuthorization(Guid adminUserId, string returnPath)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Meta OAuth não configurado (MetaOAuth no appsettings).");

        if (string.IsNullOrWhiteSpace(returnPath) || returnPath[0] != '/')
            returnPath = "/hashtags/new";

        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        _cache.Set(PendingPrefix + state, new OAuthPending(adminUserId, returnPath), PendingTtl);

        var scopesRaw = (_opt.Scopes ?? string.Empty).Replace(" ", string.Empty).Trim();
        var redirect = Uri.EscapeDataString(_opt.RedirectUri.Trim());
        var version = _opt.GraphApiVersion.Trim();
        var scopeQuery = string.IsNullOrWhiteSpace(scopesRaw)
            ? string.Empty
            : $"&scope={Uri.EscapeDataString(scopesRaw)}";

        return
            $"https://www.facebook.com/{version}/dialog/oauth?client_id={Uri.EscapeDataString(_opt.AppId.Trim())}" +
            $"&redirect_uri={redirect}&state={state}{scopeQuery}&response_type=code";
    }

    /// <summary>Callback da Meta: troca code por token longo e resolve IG Business Account ID.</summary>
    public async Task<string> HandleCallbackAsync(string? code, string? state, string? error, string? errorDescription,
        CancellationToken ct)
    {
        var feBase = _opt.FrontendBaseUrl.Trim().TrimEnd('/');

        if (!string.IsNullOrEmpty(error))
        {
            var msg = string.IsNullOrWhiteSpace(errorDescription) ? error : $"{error}: {errorDescription}";
            return AppendQuery(MakeReturnUrl(feBase, "/hashtags/new"), ("oauth_error", Uri.EscapeDataString(msg ?? "oauth_error")));
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return AppendQuery(MakeReturnUrl(feBase, "/hashtags/new"), ("oauth_error", Uri.EscapeDataString("missing_code_or_state")));

        if (!_cache.TryGetValue(PendingPrefix + state, out OAuthPending? pending) || pending is null)
        {
            _logger.LogWarning("Meta OAuth state inválido ou expirado.");
            return AppendQuery(MakeReturnUrl(feBase, "/hashtags/new"), ("oauth_error", Uri.EscapeDataString("invalid_or_expired_state")));
        }

        _cache.Remove(PendingPrefix + state);
        var returnPath = string.IsNullOrWhiteSpace(pending.ReturnPath) ? "/hashtags/new" : pending.ReturnPath;
        if (returnPath[0] != '/')
            returnPath = "/hashtags/new";

        try
        {
            var http = _httpFactory.CreateClient();
            var shortToken = await ExchangeCodeForShortLivedTokenAsync(http, code, ct);
            var longToken = await ExchangeForLongLivedTokenAsync(http, shortToken, ct);
            var igUserId = await ResolveInstagramBusinessAccountIdAsync(http, longToken, ct);

            var payload = new OAuthResultCache(pending.UserId, longToken, igUserId);
            _cache.Set(ResultPrefix + state, payload, ResultTtl);

            return AppendQuery(MakeReturnUrl(feBase, returnPath), ("oauth_state", state));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao concluir OAuth Meta.");
            return AppendQuery(MakeReturnUrl(feBase, returnPath), ("oauth_error", Uri.EscapeDataString(ex.Message)));
        }
    }

    private static string MakeReturnUrl(string frontendBase, string path) => $"{frontendBase}{path}";

    public OAuthCompleteDto? TryConsumeResult(Guid adminUserId, string state)
    {
        if (!_cache.TryGetValue(ResultPrefix + state, out OAuthResultCache? cached) || cached is null)
            return null;

        if (cached.AdminUserId != adminUserId)
            return null;

        _cache.Remove(ResultPrefix + state);
        return new OAuthCompleteDto(cached.AccessToken, cached.InstagramBusinessAccountId);
    }

    private async Task<string> ExchangeCodeForShortLivedTokenAsync(HttpClient http, string code, CancellationToken ct)
    {
        var ver = _opt.GraphApiVersion.Trim();
        var url =
            $"https://graph.facebook.com/{ver}/oauth/access_token" +
            $"?client_id={Uri.EscapeDataString(_opt.AppId.Trim())}" +
            $"&redirect_uri={Uri.EscapeDataString(_opt.RedirectUri.Trim())}" +
            $"&client_secret={Uri.EscapeDataString(_opt.AppSecret.Trim())}" +
            $"&code={Uri.EscapeDataString(code)}";

        await using var stream = await http.GetStreamAsync(url, ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out var err))
        {
            var msg = err.TryGetProperty("message", out var m) ? m.GetString() : root.GetRawText();
            throw new InvalidOperationException(msg ?? "oauth_token_exchange_failed");
        }

        if (!root.TryGetProperty("access_token", out var at))
            throw new InvalidOperationException("access_token ausente na resposta.");

        return at.GetString() ?? throw new InvalidOperationException("access_token vazio.");
    }

    private async Task<string> ExchangeForLongLivedTokenAsync(HttpClient http, string shortLived, CancellationToken ct)
    {
        var ver = _opt.GraphApiVersion.Trim();
        var url =
            $"https://graph.facebook.com/{ver}/oauth/access_token" +
            $"?grant_type=fb_exchange_token" +
            $"&client_id={Uri.EscapeDataString(_opt.AppId.Trim())}" +
            $"&client_secret={Uri.EscapeDataString(_opt.AppSecret.Trim())}" +
            $"&fb_exchange_token={Uri.EscapeDataString(shortLived)}";

        await using var stream = await http.GetStreamAsync(url, ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out var err))
        {
            var msg = err.TryGetProperty("message", out var m) ? m.GetString() : root.GetRawText();
            throw new InvalidOperationException(msg ?? "long_lived_exchange_failed");
        }

        if (!root.TryGetProperty("access_token", out var at))
            throw new InvalidOperationException("long-lived access_token ausente.");

        return at.GetString() ?? throw new InvalidOperationException("token vazio.");
    }

    private async Task<string> ResolveInstagramBusinessAccountIdAsync(HttpClient http, string accessToken,
        CancellationToken ct)
    {
        var ver = _opt.GraphApiVersion.Trim();
        var url =
            $"https://graph.facebook.com/{ver}/me/accounts" +
            $"?fields=name,instagram_business_account" +
            $"&access_token={Uri.EscapeDataString(accessToken)}";

        await using var stream = await http.GetStreamAsync(url, ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out var err))
        {
            var msg = err.TryGetProperty("message", out var m) ? m.GetString() : root.GetRawText();
            throw new InvalidOperationException(msg ?? "me_accounts_failed");
        }

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Nenhuma página encontrada para esta conta. Conecte uma página com Instagram Business.");

        foreach (var page in data.EnumerateArray())
        {
            if (!page.TryGetProperty("instagram_business_account", out var ig))
                continue;
            if (ig.TryGetProperty("id", out var idEl))
            {
                var id = idEl.GetString();
                if (!string.IsNullOrWhiteSpace(id))
                    return id;
            }
        }

        throw new InvalidOperationException(
            "Nenhuma conta Instagram Business encontrada nas páginas. Verifique se o Instagram está vinculado à página do Facebook.");
    }

    private static string AppendQuery(string baseUri, params (string Key, string Value)[] pairs)
    {
        var b = baseUri.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return baseUri + b + string.Join("&", pairs.Select(p => $"{p.Key}={p.Value}"));
    }

    private sealed record OAuthPending(Guid UserId, string ReturnPath);

    private sealed record OAuthResultCache(Guid AdminUserId, string AccessToken, string InstagramBusinessAccountId);

    public sealed record OAuthCompleteDto(string AccessToken, string InstagramBusinessAccountId);
}
