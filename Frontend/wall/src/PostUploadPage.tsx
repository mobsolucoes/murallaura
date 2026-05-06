import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from './api'

function normalizeHashtag(raw: string) {
  try {
    const d = decodeURIComponent(raw).trim()
    return d.startsWith('#') ? d.slice(1).toLowerCase() : d.toLowerCase()
  } catch {
    return raw.replace(/^#/, '').toLowerCase()
  }
}

export default function PostUploadPage() {
  const params = useParams()
  const hashtag = normalizeHashtag(params.hashtag ?? '')
  const [caption, setCaption] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [sending, setSending] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (!file) {
      setError('Selecione uma imagem ou video.')
      return
    }

    setSending(true)
    setError(null)
    setMessage(null)
    try {
      const form = new FormData()
      form.append('file', file)
      form.append('caption', caption)
      await api.post(`/api/public/wall/${encodeURIComponent(hashtag)}/submit`, form)
      setMessage('Post enviado com sucesso. Ele ja aparece no mural.')
      setCaption('')
      setFile(null)
      const input = document.getElementById('file-input') as HTMLInputElement | null
      if (input) input.value = ''
    } catch {
      setError('Nao foi possivel enviar. Verifique o arquivo e tente novamente.')
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="upload-page">
      <div className="upload-card">
        <p className="upload-breadcrumb">
          <Link to={`/wall/${hashtag}`}>Voltar ao mural</Link>
        </p>
        <h2>Postar no mural</h2>
        <p className="upload-muted">Hashtag: #{hashtag}</p>
        <form onSubmit={onSubmit} className="upload-form">
          <label>
            Arquivo (jpg, png, webp, mp4, mov, webm)
            <input id="file-input" type="file" accept=".jpg,.jpeg,.png,.webp,.mp4,.mov,.webm" onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
          </label>
          <label>
            Legenda
            <textarea rows={4} value={caption} onChange={(e) => setCaption(e.target.value)} placeholder="Escreva uma legenda..." />
          </label>
          <button className="upload-btn" type="submit" disabled={sending}>
            {sending ? 'Enviando...' : 'Publicar no mural'}
          </button>
        </form>
        {message ? <p className="upload-ok">{message}</p> : null}
        {error ? <p className="upload-error">{error}</p> : null}
      </div>
    </div>
  )
}
