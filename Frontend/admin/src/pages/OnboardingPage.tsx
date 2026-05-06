import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../api/client'
import type { HashtagConfiguration } from '../types'

type OAuthStatus = {
  enabled: boolean
  missingConfiguration?: string[]
}

export default function OnboardingPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const [searchParams, setSearchParams] = useSearchParams()

  const [step, setStep] = useState(1)
  const [oauthReady, setOauthReady] = useState(false)
  const [oauthMissing, setOauthMissing] = useState<string[]>([])
  const [oauthLoaded, setOauthLoaded] = useState(false)

  const [instagramBusinessAccountId, setIgbId] = useState('')
  const [metaAccessToken, setMetaAccessToken] = useState('')
  const [hashtag, setHashtag] = useState('')
  const [pollIntervalMinutes, setPoll] = useState(5)
  const [autoApprovePosts, setAutoApprovePosts] = useState(false)
  const [isMonitoringEnabled, setMonitor] = useState(true)

  const [info, setInfo] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    api
      .get<OAuthStatus>('/api/admin/meta/oauth/status')
      .then((r) => {
        setOauthReady(r.data.enabled)
        setOauthMissing(Array.isArray(r.data.missingConfiguration) ? r.data.missingConfiguration : [])
      })
      .catch(() => {
        setOauthReady(false)
        setOauthMissing(['Não foi possível consultar a API (backend parado ou URL incorreta).'])
      })
      .finally(() => setOauthLoaded(true))
  }, [])

  useEffect(() => {
    const errQ = searchParams.get('oauth_error')
    const state = searchParams.get('oauth_state')

    if (errQ) {
      try {
        setError(decodeURIComponent(errQ))
      } catch {
        setError(errQ)
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
        setError(null)
        setInfo('Conta conectada com sucesso. Agora escolha a hashtag.')
        setStep(2)
      } catch {
        if (!cancelled) setError('Não foi possível concluir a autorização. Tente novamente.')
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
    setError(null)
    setInfo(null)
    try {
      const { data } = await api.post<{ authorizationUrl: string }>('/api/admin/meta/oauth/start', {
        returnTo: `${location.pathname}${location.search}`,
      })
      window.location.href = data.authorizationUrl
    } catch {
      setError('OAuth Meta não disponível. Configure AppId e AppSecret no backend.')
    }
  }

  async function createAndStart() {
    setSaving(true)
    setError(null)
    try {
      const { data } = await api.post<HashtagConfiguration>('/api/admin/hashtags', {
        hashtag,
        instagramBusinessAccountId,
        metaAccessToken: metaAccessToken.trim(),
        pollIntervalMinutes,
        autoApprovePosts,
        isMonitoringEnabled,
      })
      navigate(`/hashtags/${data.id}/moderate`)
    } catch (e: any) {
      setError(e?.response?.data?.error ? `Erro: ${e.response.data.error}` : 'Não foi possível criar a hashtag.')
    } finally {
      setSaving(false)
    }
  }

  const canNextToStep3 = hashtag.trim() !== '' && instagramBusinessAccountId.trim() !== '' && metaAccessToken.trim() !== ''

  return (
    <div className="page narrow">
      <header className="page-head">
        <div>
          <p className="muted">
            <Link to="/dashboard">Dashboard</Link> / Onboarding
          </p>
          <h2>Configuração rápida do mural</h2>
          <p className="muted">Fluxo guiado para conectar Instagram, escolher hashtag e começar.</p>
        </div>
      </header>

      <div className="wizard-steps">
        <span className={`wizard-step ${step >= 1 ? 'active' : ''}`}>1. Conectar Meta</span>
        <span className={`wizard-step ${step >= 2 ? 'active' : ''}`}>2. Hashtag</span>
        <span className={`wizard-step ${step >= 3 ? 'active' : ''}`}>3. Revisar</span>
      </div>

      {step === 1 && (
        <section className="card stack">
          <h3>Conecte sua conta</h3>
          <p className="muted">
            Você autoriza no login oficial da Meta. O sistema preenche automaticamente token e IG User ID.
          </p>
          {!oauthLoaded && <p className="muted sm">Verificando configuração OAuth…</p>}
          {oauthLoaded && !oauthReady && (
            <div className="oauth-warn">
              <strong>Falta configurar no servidor:</strong>
              <ul>
                {oauthMissing.map((m) => (
                  <li key={m}>{m}</li>
                ))}
              </ul>
            </div>
          )}
          <div className="actions-row">
            <button className="btn secondary" disabled={!oauthLoaded || !oauthReady} onClick={() => void connectMeta()}>
              Autorizar com Facebook / Instagram
            </button>
            <button
              className="btn ghost"
              disabled={!instagramBusinessAccountId || !metaAccessToken}
              onClick={() => setStep(2)}
            >
              Já conectei, continuar
            </button>
          </div>
          {info && <p className="success">{info}</p>}
          {error && <p className="error">{error}</p>}
        </section>
      )}

      {step === 2 && (
        <section className="card stack">
          <h3>Escolha hashtag e frequência</h3>
          <label className="field">
            <span>Hashtag (sem #)</span>
            <input value={hashtag} onChange={(e) => setHashtag(e.target.value)} placeholder="meuevento" />
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
            <input type="checkbox" checked={autoApprovePosts} onChange={(e) => setAutoApprovePosts(e.target.checked)} />
            <span>Aprovar posts automaticamente (sem moderação)</span>
          </label>
          <label className="field row">
            <input type="checkbox" checked={isMonitoringEnabled} onChange={(e) => setMonitor(e.target.checked)} />
            <span>Ativar monitoramento imediatamente</span>
          </label>
          <div className="actions-row">
            <button className="btn ghost" onClick={() => setStep(1)}>
              Voltar
            </button>
            <button className="btn primary" disabled={!canNextToStep3} onClick={() => setStep(3)}>
              Continuar
            </button>
          </div>
        </section>
      )}

      {step === 3 && (
        <section className="card stack">
          <h3>Revisar e iniciar</h3>
          <div className="wizard-review">
            <div>
              <small>Conta Instagram (IG User ID)</small>
              <strong>{instagramBusinessAccountId || '—'}</strong>
            </div>
            <div>
              <small>Hashtag</small>
              <strong>#{hashtag || '—'}</strong>
            </div>
            <div>
              <small>Intervalo</small>
              <strong>{pollIntervalMinutes} min</strong>
            </div>
            <div>
              <small>Aprovação automática</small>
              <strong>{autoApprovePosts ? 'Sim' : 'Não'}</strong>
            </div>
            <div>
              <small>Monitoramento</small>
              <strong>{isMonitoringEnabled ? 'Ativo' : 'Desativado'}</strong>
            </div>
          </div>
          <p className="muted sm">
            Ao confirmar, a hashtag será criada e você vai direto para a moderação para aprovar as primeiras fotos.
          </p>
          <div className="actions-row">
            <button className="btn ghost" onClick={() => setStep(2)}>
              Voltar
            </button>
            <button className="btn primary" disabled={saving || !canNextToStep3} onClick={() => void createAndStart()}>
              {saving ? 'Criando…' : 'Criar hashtag e iniciar'}
            </button>
          </div>
          {error && <p className="error">{error}</p>}
        </section>
      )}
    </div>
  )
}
