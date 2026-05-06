import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { api } from '../api/client'
import type { HashtagConfiguration } from '../types'

export default function HashtagFormPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const location = useLocation()
  const [searchParams, setSearchParams] = useSearchParams()
  const isEdit = Boolean(id && id !== 'new')

  const [hashtag, setHashtag] = useState('')
  const [instagramBusinessAccountId, setIgbId] = useState('')
  const [metaAccessToken, setMetaAccessToken] = useState('')
  const [pollIntervalMinutes, setPoll] = useState(5)
  const [autoApprovePosts, setAutoApprovePosts] = useState(false)
  const [isMonitoringEnabled, setMonitor] = useState(true)
  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)

  const [metaOAuthReady, setMetaOAuthReady] = useState(false)
  const [metaOAuthMissing, setMetaOAuthMissing] = useState<string[]>([])
  const [oauthStatusLoaded, setOauthStatusLoaded] = useState(false)
  const [oauthInfo, setOauthInfo] = useState<string | null>(null)
  const [oauthError, setOauthError] = useState<string | null>(null)

  useEffect(() => {
    if (!isEdit || !id) return
    let cancelled = false
    ;(async () => {
      try {
        const { data } = await api.get<HashtagConfiguration>(`/api/admin/hashtags/${id}`)
        if (cancelled) return
        setHashtag(data.hashtag)
        setIgbId(data.instagramBusinessAccountId)
        setPoll(data.pollIntervalMinutes)
        setAutoApprovePosts(data.autoApprovePosts)
        setMonitor(data.isMonitoringEnabled)
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [id, isEdit])

  useEffect(() => {
    api
      .get<{ enabled: boolean; missingConfiguration?: string[] }>('/api/admin/meta/oauth/status')
      .then((r) => {
        setMetaOAuthReady(r.data.enabled)
        setMetaOAuthMissing(Array.isArray(r.data.missingConfiguration) ? r.data.missingConfiguration : [])
      })
      .catch(() => {
        setMetaOAuthReady(false)
        setMetaOAuthMissing(['Não foi possível consultar a API (backend parado ou URL incorreta).'])
      })
      .finally(() => setOauthStatusLoaded(true))
  }, [])

  useEffect(() => {
    const errQ = searchParams.get('oauth_error')
    const state = searchParams.get('oauth_state')

    if (errQ) {
      try {
        setOauthError(decodeURIComponent(errQ))
      } catch {
        setOauthError(errQ)
      }
      const next = new URLSearchParams(searchParams)
      next.delete('oauth_error')
      setSearchParams(next, { replace: true })
      return
    }

    if (!state) return

    let cancelled = false
    ;(async () => {
      try {
        const { data } = await api.get<{ accessToken: string; instagramBusinessAccountId: string }>(
          '/api/admin/meta/oauth/complete',
          { params: { state } }
        )
        if (cancelled) return
        setIgbId(data.instagramBusinessAccountId)
        setMetaAccessToken(data.accessToken)
        setOauthError(null)
        setOauthInfo('Conectado à Meta: token e IG User ID preenchidos. Revise e salve.')
      } catch {
        if (!cancelled)
          setOauthError('Não foi possível concluir a autorização. Tente novamente ou preencha manualmente.')
      } finally {
        const next = new URLSearchParams(searchParams)
        next.delete('oauth_state')
        if (!cancelled) setSearchParams(next, { replace: true })
      }
    })()

    return () => {
      cancelled = true
    }
  }, [searchParams, setSearchParams])

  async function connectMeta() {
    setOauthError(null)
    setOauthInfo(null)
    try {
      const { data } = await api.post<{ authorizationUrl: string }>('/api/admin/meta/oauth/start', {
        returnTo: `${location.pathname}${location.search}`,
      })
      window.location.href = data.authorizationUrl
    } catch {
      setOauthError(
        'OAuth Meta não disponível. Configure AppId, AppSecret e URIs em MetaOAuth (servidor) ou preencha manualmente.'
      )
    }
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    try {
      const body = {
        hashtag,
        instagramBusinessAccountId,
        metaAccessToken: metaAccessToken.trim() === '' ? null : metaAccessToken,
        pollIntervalMinutes,
        autoApprovePosts,
        isMonitoringEnabled,
      }
      if (isEdit && id) {
        await api.put(`/api/admin/hashtags/${id}`, body)
        navigate(`/hashtags/${id}/moderate`)
      } else {
        const { data } = await api.post<HashtagConfiguration>('/api/admin/hashtags', {
          ...body,
          metaAccessToken: metaAccessToken.trim(),
        })
        navigate(`/hashtags/${data.id}/moderate`)
      }
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <p className="muted page">Carregando…</p>

  return (
    <div className="page narrow">
      <header className="page-head">
        <div>
          <p className="muted">
            <Link to="/dashboard">Dashboard</Link> / Hashtag
          </p>
          <h2>{isEdit ? 'Editar hashtag' : 'Nova hashtag'}</h2>
        </div>
      </header>

      <div className="card stack oauth-card">
        <h3 className="oauth-title">Conectar com a Meta (recomendado)</h3>
        <p className="muted sm oauth-copy">
          Não é possível (nem permitido) pedir usuário e senha do Instagram aqui. O fluxo oficial abre a página da
          Meta/Facebook: você autoriza o app e o sistema preenche automaticamente o <strong>IG User ID</strong> e o{' '}
          <strong>token de acesso</strong> (válido por um período; depois faça reautorização ou use token de longa
          duração conforme a documentação da Meta).
        </p>
        <ol className="oauth-steps sm muted">
          <li>
            Configure <code>MetaOAuth</code> em <code>Backend/Api/appsettings.json</code> (AppId, AppSecret, RedirectUri
            igual ao app Meta, FrontendBaseUrl).
          </li>
          <li>Clique no botão abaixo, faça login com a conta que administra a página/Instagram Business.</li>
          <li>Revise os campos e salve a hashtag.</li>
        </ol>
        {!oauthStatusLoaded && <p className="muted sm">Verificando configuração OAuth…</p>}
        {oauthStatusLoaded && !metaOAuthReady && metaOAuthMissing.length > 0 && (
          <div className="oauth-warn">
            <strong>Botão desativado até configurar no servidor:</strong>
            <ul>
              {metaOAuthMissing.map((m) => (
                <li key={m}>{m}</li>
              ))}
            </ul>
          </div>
        )}
        {oauthInfo && <p className="success">{oauthInfo}</p>}
        {oauthError && <p className="error">{oauthError}</p>}
        <div className="actions-row">
          <button
            type="button"
            className="btn secondary"
            disabled={!oauthStatusLoaded || !metaOAuthReady}
            onClick={() => void connectMeta()}
            title={!metaOAuthReady ? 'Preencha AppId e AppSecret em MetaOAuth no backend' : undefined}
          >
            Autorizar com Facebook / Instagram (Meta)
          </button>
        </div>
      </div>

      <form className="card stack" onSubmit={onSubmit}>
        <label className="field">
          <span>Hashtag (sem #)</span>
          <input value={hashtag} onChange={(e) => setHashtag(e.target.value)} required placeholder="meuevento" />
        </label>
        <label className="field">
          <span>Instagram Business Account ID (IG User ID)</span>
          <input
            value={instagramBusinessAccountId}
            onChange={(e) => setIgbId(e.target.value)}
            required
            placeholder="1784…"
          />
        </label>
        <label className="field">
          <span>Token de acesso Meta (Graph API)</span>
          <textarea
            value={metaAccessToken}
            onChange={(e) => setMetaAccessToken(e.target.value)}
            rows={4}
            placeholder={isEdit ? 'Deixe em branco para manter o token atual' : 'EAAG…'}
          />
          <small className="muted">Nunca é exibido no navegador após salvo.</small>
        </label>
        <label className="field">
          <span>Intervalo de busca (minutos)</span>
          <input
            type="number"
            min={1}
            max={1440}
            value={pollIntervalMinutes}
            onChange={(e) => setPoll(Number(e.target.value))}
          />
        </label>
        <label className="field row">
          <input
            type="checkbox"
            checked={autoApprovePosts}
            onChange={(e) => setAutoApprovePosts(e.target.checked)}
          />
          <span>Aprovar posts automaticamente (sem moderação)</span>
        </label>
        <label className="field row">
          <input
            type="checkbox"
            checked={isMonitoringEnabled}
            onChange={(e) => setMonitor(e.target.checked)}
          />
          <span>Monitoramento ativo</span>
        </label>
        <div className="actions-row">
          <button className="btn primary" type="submit" disabled={saving}>
            {saving ? 'Salvando…' : 'Salvar'}
          </button>
          <Link className="btn ghost" to="/dashboard">
            Cancelar
          </Link>
        </div>
      </form>
    </div>
  )
}
