import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { HashtagConfiguration, WallConfiguration } from '../types'

export default function WallVisualPage() {
  const { id } = useParams<{ id: string }>()
  const [cfg, setCfg] = useState<WallConfiguration | null>(null)
  const [tagCfg, setTagCfg] = useState<HashtagConfiguration | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  const [title, setTitle] = useState('')
  const [logoUrl, setLogoUrl] = useState('')
  const [primaryColor, setPrimary] = useState('#6366f1')
  const [secondaryColor, setSecondary] = useState('#1e1b4b')
  const [accentColor, setAccent] = useState('#f472b6')
  const [theme, setTheme] = useState('dark')
  const [displayDurationSeconds, setDuration] = useState(30)
  const [showCaption, setShowCaption] = useState(true)
  const [showQrCode, setShowQr] = useState(true)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    ;(async () => {
      try {
        const [wallRes, tagRes] = await Promise.all([
          api.get<WallConfiguration>(`/api/admin/hashtags/${id}/wall`),
          api.get<HashtagConfiguration>(`/api/admin/hashtags/${id}`),
        ])
        if (cancelled) return
        const data = wallRes.data
        setCfg(data)
        setTagCfg(tagRes.data)
        setTitle(data.title)
        setLogoUrl(data.logoUrl ?? '')
        setPrimary(data.primaryColor)
        setSecondary(data.secondaryColor)
        setAccent(data.accentColor)
        setTheme(data.theme)
        setDuration(data.displayDurationSeconds)
        setShowCaption(data.showCaption)
        setShowQr(data.showQrCode)
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [id])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (!id) return
    setSaving(true)
    try {
      await api.put(`/api/admin/hashtags/${id}/wall`, {
        title,
        logoUrl: logoUrl.trim() === '' ? null : logoUrl.trim(),
        primaryColor,
        secondaryColor,
        accentColor,
        theme,
        displayDurationSeconds,
        showCaption,
        showQrCode,
      })
      const { data } = await api.get<WallConfiguration>(`/api/admin/hashtags/${id}/wall`)
      setCfg(data)
    } finally {
      setSaving(false)
    }
  }

  if (loading || !cfg || !tagCfg) return <p className="muted page">Carregando…</p>

  const previewUrl = buildWallUrl(tagCfg.hashtag)

  return (
    <div className="page narrow">
      <header className="page-head">
        <div>
          <p className="muted">
            <Link to="/dashboard">Dashboard</Link> / Visual do mural
          </p>
          <h2>Aparência do mural</h2>
        </div>
        <a className="btn secondary" href={previewUrl} target="_blank" rel="noreferrer">
          Preview em tela cheia
        </a>
      </header>

      <form className="card stack" onSubmit={onSubmit}>
        <label className="field">
          <span>Título no topo</span>
          <input value={title} onChange={(e) => setTitle(e.target.value)} required />
        </label>
        <label className="field">
          <span>URL do logo (opcional)</span>
          <input value={logoUrl} onChange={(e) => setLogoUrl(e.target.value)} placeholder="https://…" />
        </label>
        <div className="color-grid">
          <label className="field">
            <span>Cor primária</span>
            <input type="color" value={primaryColor} onChange={(e) => setPrimary(e.target.value)} />
          </label>
          <label className="field">
            <span>Cor de fundo</span>
            <input type="color" value={secondaryColor} onChange={(e) => setSecondary(e.target.value)} />
          </label>
          <label className="field">
            <span>Destaque</span>
            <input type="color" value={accentColor} onChange={(e) => setAccent(e.target.value)} />
          </label>
        </div>
        <label className="field">
          <span>Tema</span>
          <select value={theme} onChange={(e) => setTheme(e.target.value)}>
            <option value="dark">Escuro</option>
            <option value="light">Claro</option>
          </select>
        </label>
        <label className="field">
          <span>Tempo por foto (segundos)</span>
          <input
            type="number"
            min={5}
            max={3600}
            value={displayDurationSeconds}
            onChange={(e) => setDuration(Number(e.target.value))}
          />
        </label>
        <label className="field row">
          <input type="checkbox" checked={showCaption} onChange={(e) => setShowCaption(e.target.checked)} />
          <span>Mostrar legenda resumida</span>
        </label>
        <label className="field row">
          <input type="checkbox" checked={showQrCode} onChange={(e) => setShowQr(e.target.checked)} />
          <span>Mostrar QR Code</span>
        </label>
        <div className="actions-row">
          <button className="btn primary" type="submit" disabled={saving}>
            {saving ? 'Salvando…' : 'Salvar'}
          </button>
        </div>
      </form>
    </div>
  )
}

function buildWallUrl(hashtag: string) {
  const origin =
    import.meta.env.VITE_WALL_ORIGIN ??
    (typeof window !== 'undefined'
      ? `${window.location.protocol}//${window.location.hostname}:5174`
      : 'http://localhost:5174')
  return `${origin}/wall/${encodeURIComponent(hashtag)}`
}
