import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { InstagramMediaPost } from '../types'

export default function ModerationPage() {
  const { id } = useParams<{ id: string }>()
  const [items, setItems] = useState<InstagramMediaPost[]>([])
  const [loading, setLoading] = useState(true)
  const [downloading, setDownloading] = useState(false)
  const [selected, setSelected] = useState<string[]>([])

  async function reload() {
    if (!id) return
    const { data } = await api.get<InstagramMediaPost[]>(`/api/admin/hashtags/${id}/media`)
    setItems(data)
    setSelected((prev) => prev.filter((x) => data.some((m) => m.id === x)))
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

  async function deleteOne(postId: string) {
    await api.delete(`/api/admin/media/${postId}`)
    await reload()
  }

  async function deleteSelected() {
    if (!id || selected.length === 0) return
    await api.post('/api/admin/media/bulk-delete', {
      hashtagConfigId: id,
      postIds: selected,
    })
    await reload()
    setSelected([])
  }

  async function syncNow() {
    if (!id) return
    await api.post(`/api/admin/hashtags/${id}/sync`)
    await reload()
  }

  async function downloadAllPhotos() {
    if (!id) return
    setDownloading(true)
    try {
      const { data, headers } = await api.get<Blob>(`/api/admin/hashtags/${id}/media/download`, {
        responseType: 'blob',
      })
      const disposition = headers['content-disposition'] as string | undefined
      const match = disposition?.match(/filename="?([^";]+)"?/i)
      const fileName = match?.[1] ?? `fotos-${id}.zip`
      const url = URL.createObjectURL(data)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      link.click()
      URL.revokeObjectURL(url)
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status
      if (status === 404) {
        window.alert('Nenhuma foto disponível para download.')
      } else {
        window.alert('Não foi possível baixar as fotos. Tente novamente.')
      }
    } finally {
      setDownloading(false)
    }
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

  const allSelected = items.length > 0 && selected.length === items.length

  function toggleSelect(postId: string) {
    setSelected((prev) => (prev.includes(postId) ? prev.filter((x) => x !== postId) : [...prev, postId]))
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
          <button
            type="button"
            className="btn ghost"
            onClick={() => setSelected(allSelected ? [] : items.map((m) => m.id))}
            disabled={items.length === 0}
          >
            {allSelected ? 'Desmarcar todos' : 'Selecionar todos'}
          </button>
          <button type="button" className="btn danger" onClick={() => deleteSelected()} disabled={selected.length === 0}>
            Excluir selecionados ({selected.length})
          </button>
          <button type="button" className="btn secondary" onClick={() => syncNow()}>
            Buscar agora (Instagram)
          </button>
          <button
            type="button"
            className="btn secondary"
            onClick={() => downloadAllPhotos()}
            disabled={downloading || items.length === 0}
          >
            {downloading ? 'Preparando ZIP…' : 'Baixar todas as fotos'}
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
                <label className="media-select">
                  <input
                    type="checkbox"
                    checked={selected.includes(m.id)}
                    onChange={() => toggleSelect(m.id)}
                    aria-label={`Selecionar post ${m.id}`}
                  />
                </label>
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
                  <button className="btn danger sm" type="button" onClick={() => deleteOne(m.id)}>
                    Excluir
                  </button>
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
