using System.Security.Claims;
using HashtagWall.Api.Options;
using HashtagWall.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HashtagWall.Api.Controllers;

/// <summary>OAuth oficial Meta (Facebook Login) para obter token Graph + IG User ID.</summary>
[ApiController]
public sealed class MetaOAuthAdminController : ControllerBase
{
    private readonly MetaOAuthFlow _flow;
    private readonly IOptions<MetaOAuthOptions> _opt;

    public MetaOAuthAdminController(MetaOAuthFlow flow, IOptions<MetaOAuthOptions> opt)
    {
        _flow = flow;
        _opt = opt;
    }

    [Authorize]
    [HttpGet("/api/admin/meta/oauth/status")]
    public ActionResult<object> Status()
    {
        var o = _opt.Value;
        var missing = new List<string>();
        if (!o.Enabled)
            missing.Add("MetaOAuth.Enabled está false no appsettings");
        if (string.IsNullOrWhiteSpace(o.AppId))
            missing.Add("AppId");
        if (string.IsNullOrWhiteSpace(o.AppSecret))
            missing.Add("AppSecret");
        if (string.IsNullOrWhiteSpace(o.RedirectUri))
            missing.Add("RedirectUri");
        if (string.IsNullOrWhiteSpace(o.FrontendBaseUrl))
            missing.Add("FrontendBaseUrl");

        return Ok(new
        {
            enabled = _flow.IsConfigured,
            missingConfiguration = missing,
            appId = string.IsNullOrWhiteSpace(o.AppId) ? null : Mask(o.AppId),
            redirectUri = string.IsNullOrWhiteSpace(o.RedirectUri) ? null : o.RedirectUri,
            frontendBaseUrl = string.IsNullOrWhiteSpace(o.FrontendBaseUrl) ? null : o.FrontendBaseUrl,
            scopes = o.Scopes,
            graphApiVersion = o.GraphApiVersion
        });
    }

    /// <summary>Inicia fluxo: redirecione o navegador para <c>authorizationUrl</c>.</summary>
    [Authorize]
    [HttpPost("/api/admin/meta/oauth/start")]
    public ActionResult<object> Start([FromBody] MetaOAuthStartRequest? body)
    {
        if (!_flow.IsConfigured)
            return BadRequest(new
            {
                error = "meta_oauth_disabled",
                message = "Configure MetaOAuth no servidor (AppId, AppSecret, RedirectUri, FrontendBaseUrl)."
            });

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var returnTo = string.IsNullOrWhiteSpace(body?.ReturnTo) ? "/hashtags/new" : body!.ReturnTo.Trim();
        if (!returnTo.StartsWith('/'))
            returnTo = "/hashtags/new";

        var url = _flow.BeginAuthorization(userId, returnTo);
        return Ok(new { authorizationUrl = url });
    }

    public sealed record MetaOAuthStartRequest(string? ReturnTo);

    /// <summary>Consome resultado único após callback (preenche token + IG ID no admin).</summary>
    [Authorize]
    [HttpGet("/api/admin/meta/oauth/complete")]
    public ActionResult<object> Complete([FromQuery] string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return BadRequest(new { error = "missing_state" });

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var dto = _flow.TryConsumeResult(userId, state.Trim());
        if (dto is null)
            return NotFound(new { error = "result_not_found_or_expired" });

        return Ok(new
        {
            accessToken = dto.AccessToken,
            instagramBusinessAccountId = dto.InstagramBusinessAccountId
        });
    }

    private static string Mask(string s) =>
        s.Length <= 6 ? "****" : $"{s[..4]}…{s[^4..]}";
}

[ApiController]
[AllowAnonymous]
public sealed class MetaOAuthPublicController : ControllerBase
{
    private readonly MetaOAuthFlow _flow;

    public MetaOAuthPublicController(MetaOAuthFlow flow) => _flow = flow;

    /// <summary>Callback configurado no Meta Developer Console.</summary>
    [HttpGet("/api/public/meta/oauth/callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery(Name = "error")] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        CancellationToken ct)
    {
        var redirect = await _flow.HandleCallbackAsync(code, state, error, errorDescription, ct);
        return Redirect(redirect);
    }
}
