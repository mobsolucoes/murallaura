namespace HashtagWall.Api.Options;

public sealed class MetaOAuthOptions
{
    public const string SectionName = "MetaOAuth";

    /// <summary>When false, OAuth endpoints return disabled.</summary>
    public bool Enabled { get; set; }

    public string AppId { get; set; } = string.Empty;

    /// <summary>App Secret — servidor apenas.</summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>Versão da Graph API usada no diálogo OAuth e troca de tokens.</summary>
    public string GraphApiVersion { get; set; } = "v21.0";

    /// <summary>Escopos separados por vírgula (Facebook Login).</summary>
    public string Scopes { get; set; } = "pages_show_list,instagram_basic,business_management";

    /// <summary>Uri registrado no app Meta — deve bater exatamente com o configurado no Developer Console.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Origem do painel (ex.: http://localhost:5173). O retorno do OAuth usa ReturnTo + query.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
