import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { IntegrationLog } from '../types'

export default function LogsPage() {
  const { id } = useParams<{ id: string }>()
  const [items, setItems] = useState<IntegrationLog[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!id) return
    let cancelled = false
    ;(async () => {
      try {
        const { data } = await api.get<IntegrationLog[]>('/api/admin/integration-logs', {
          params: { hashtagConfigurationId: id, take: 200 },
        })
        if (!cancelled) setItems(data)
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [id])

  const levelName = (l: number) => {
    if (l === 1) return 'Warning'
    if (l === 2) return 'Error'
    return 'Info'
  }

  return (
    <div className="page">
      <header className="page-head">
        <div>
          <p className="muted">
            <Link to="/dashboard">Dashboard</Link> / Logs de integração
          </p>
          <h2>Logs</h2>
        </div>
      </header>

      {loading ? (
        <p className="muted">Carregando…</p>
      ) : (
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Quando</th>
                <th>Nível</th>
                <th>Mensagem</th>
              </tr>
            </thead>
            <tbody>
              {items.map((l) => (
                <tr key={l.id}>
                  <td className="nowrap">{new Date(l.createdAt).toLocaleString()}</td>
                  <td>
                    <span className={`pill ${l.level === 2 ? 'bad' : l.level === 1 ? 'warn' : ''}`}>
                      {levelName(l.level)}
                    </span>
                  </td>
                  <td>
                    <div>{l.message}</div>
                    {l.details && <pre className="log-details">{l.details}</pre>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {items.length === 0 && <p className="muted pad">Nenhum log ainda.</p>}
        </div>
      )}
    </div>
  )
}
