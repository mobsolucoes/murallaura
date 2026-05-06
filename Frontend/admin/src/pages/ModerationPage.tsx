import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { InstagramMediaPost } from '../types'

export default function ModerationPage() {
  const { id } = useParams<{ id: string }>()
  const [items, setItems] = useState<InstagramMediaPost[]>([])
  const [loading, setLoading] = useState(true)

  async function reload() {
    if (!id) return
    const { data } = await api.get<InstagramMediaPost[]>(`/api/admin/hashtags/${id}/media`)
    setItems(data)
  }

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        await reload()
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    const t = window.setInterval(() => {
      reload().catch(() => {})
    }, 25000)
    return () => {
      cancelled = true
      window.clearInterval(t)
    }
  }, [id])

  async function approve(postId: string) {
    await api.post(`/api/admin/media/${postId}/approve`)
    await reload()
  }

  async function reject(postId: string) {
    await api.post(`/api/admin/media/${postId}/reject`)
    await reload()
  }

  async function syncNow() {
    if (!id) return
    await api.post(`/api/admin/hashtags/${id}/sync`)
    await reload()
  }

  const statusLabel = (s: number) => {
    if (s === 1) return 'Aprovada'
    if (s === 2) return 'Rejeitada'
    return 'Pendente'
  }

  const statusClass = (s: number) => {
    if (s === 1) return 'ok'
    if (s === 2) return 'bad'
    return 'warn'
  }

  return (
    <div className="page">
      <header className="page-head">
        <div>
          <p className="muted">
            <Link to="/dashboard">Dashboard</Link> / Moderação
          </p>
          <h2>Fila de moderação</h2>
        </div>
        <div className="actions-row">
          <button type="button" className="btn secondary" onClick={() => syncNow()}>
            Buscar agora (Instagram)
          </button>
          <Link className="btn ghost" to={`/hashtags/${id}/wall`}>
            Visual do mural
          </Link>
        </div>
      </header>

      {loading ? (
        <p className="muted">Carregando…</p>
      ) : (
        <div className="mod-grid">
          {items.map((m) => (
            <article key={m.id} className="card mod-card">
              <div className="thumb">
                <img src={(m.mediaUrl ?? m.thumbnailUrl) ?? ''} alt="" loading="lazy" />
                <span className={`pill ${statusClass(m.status)}`}>{statusLabel(m.status)}</span>
              </div>
              <div className="mod-body">
                <p className="caption">{m.caption?.slice(0, 160) ?? 'Sem legenda'}</p>
                <div className="mod-actions">
                  <button className="btn primary sm" type="button" onClick={() => approve(m.id)}>
                    Aprovar
                  </button>
                  <button className="btn secondary sm" type="button" onClick={() => reject(m.id)}>
                    Reprovar
                  </button>
                  <a className="btn ghost sm" href={m.permalink} target="_blank" rel="noreferrer">
                    Instagram
                  </a>
                </div>
              </div>
            </article>
          ))}
          {items.length === 0 && (
            <div className="card empty">
              <p>Nenhuma publicação sincronizada ainda. Use “Buscar agora” ou aguarde o worker.</p>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
