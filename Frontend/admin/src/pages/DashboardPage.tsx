import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { DashboardStats, HashtagConfiguration } from '../types'

export default function DashboardPage() {
  const [hashtags, setHashtags] = useState<HashtagConfiguration[]>([])
  const [statsMap, setStatsMap] = useState<Record<string, DashboardStats>>({})
  const [loading, setLoading] = useState(true)
  const [deletingId, setDeletingId] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const { data } = await api.get<HashtagConfiguration[]>('/api/admin/hashtags')
        if (cancelled) return
        setHashtags(data)
        const entries = await Promise.all(
          data.map(async (h) => {
            const r = await api.get<DashboardStats>(`/api/admin/hashtags/${h.id}/dashboard`)
            return [h.id, r.data] as const
          })
        )
        if (cancelled) return
        const map: Record<string, DashboardStats> = {}
        for (const [id, s] of entries) map[id] = s
        setStatsMap(map)
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  async function handleDelete(id: string, hashtag: string) {
    const confirmed = window.confirm(`Excluir a hashtag #${hashtag}? Essa ação não pode ser desfeita.`)
    if (!confirmed) return

    try {
      setDeletingId(id)
      await api.delete(`/api/admin/hashtags/${id}`)
      setHashtags((prev) => prev.filter((h) => h.id !== id))
      setStatsMap((prev) => {
        const next = { ...prev }
        delete next[id]
        return next
      })
    } finally {
      setDeletingId(null)
    }
  }

  return (
    <div className="page">
      <header className="page-head">
        <div>
          <h2>Dashboard</h2>
          <p className="muted">Visão geral das hashtags monitoradas.</p>
        </div>
        <div className="actions-row">
          <Link className="btn secondary" to="/onboarding">
            Setup guiado
          </Link>
          <Link className="btn primary" to="/hashtags/new">
            Nova hashtag
          </Link>
        </div>
      </header>

      {loading ? (
        <p className="muted">Carregando…</p>
      ) : hashtags.length === 0 ? (
        <div className="card empty">
          <p>Nenhuma hashtag cadastrada.</p>
          <Link className="btn secondary" to="/onboarding">
            Começar com setup guiado
          </Link>
        </div>
      ) : (
        <div className="grid-cards">
          {hashtags.map((h) => {
            const s = statsMap[h.id]
            return (
              <article key={h.id} className="card hashtag-card">
                <div className="hashtag-card-head">
                  <span className="pill hash">#{h.hashtag}</span>
                  <span className={`pill ${h.isMonitoringEnabled ? 'ok' : 'off'}`}>
                    {h.isMonitoringEnabled ? 'Ativo' : 'Pausado'}
                  </span>
                </div>
                <div className="stats-row">
                  <div>
                    <small>Capturadas</small>
                    <strong>{s?.totalCaptured ?? '—'}</strong>
                  </div>
                  <div>
                    <small>Aprovadas</small>
                    <strong>{s?.approved ?? '—'}</strong>
                  </div>
                  <div>
                    <small>Pendentes</small>
                    <strong>{s?.pending ?? '—'}</strong>
                  </div>
                  <div>
                    <small>Rejeitadas</small>
                    <strong>{s?.rejected ?? '—'}</strong>
                  </div>
                </div>
                <div className="actions-row">
                  <Link className="btn ghost sm" to={`/hashtags/${h.id}/moderate`}>
                    Moderação
                  </Link>
                  <Link className="btn ghost sm" to={`/hashtags/${h.id}/wall`}>
                    Visual
                  </Link>
                  <Link className="btn ghost sm" to={`/hashtags/${h.id}/logs`}>
                    Logs
                  </Link>
                  <Link className="btn ghost sm" to={`/hashtags/${h.id}/edit`}>
                    Editar
                  </Link>
                  <button
                    className="btn ghost sm"
                    onClick={() => handleDelete(h.id, h.hashtag)}
                    disabled={deletingId === h.id}
                  >
                    {deletingId === h.id ? 'Excluindo…' : 'Excluir'}
                  </button>
                </div>
              </article>
            )
          })}
        </div>
      )}
    </div>
  )
}
