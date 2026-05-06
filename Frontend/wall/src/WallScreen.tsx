import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { QRCodeSVG } from 'qrcode.react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import type { CSSProperties } from 'react'
import { useParams } from 'react-router-dom'
import { api, apiBaseUrl } from './api'
import type { InstagramMediaPost, WallConfiguration } from './types'

function normalizeHashtag(raw: string) {
  try {
    const d = decodeURIComponent(raw).trim()
    return d.startsWith('#') ? d.slice(1).toLowerCase() : d.toLowerCase()
  } catch {
    return raw.replace(/^#/, '').toLowerCase()
  }
}

export default function WallScreen() {
  const params = useParams()
  const rawTag = params.hashtag ?? ''

  const hashtag = normalizeHashtag(rawTag)

  const [cfg, setCfg] = useState<WallConfiguration | null>(null)
  const [posts, setPosts] = useState<InstagramMediaPost[]>([])
  const [index, setIndex] = useState(0)
  const [visible, setVisible] = useState(true)

  const durationMs = Math.max(5, cfg?.displayDurationSeconds ?? 30) * 1000

  const qrValue = useMemo(
    () => `https://www.instagram.com/explore/tags/${encodeURIComponent(hashtag)}/`,
    [hashtag]
  )

  const loadAll = useCallback(async () => {
    const tag = hashtag
    const [c, p] = await Promise.all([
      api.get<WallConfiguration>(`/api/public/wall/${encodeURIComponent(tag)}/config`),
      api.get<InstagramMediaPost[]>(`/api/public/wall/${encodeURIComponent(tag)}/approved-posts`),
    ])
    setCfg(c.data)
    setPosts(p.data)
    setIndex(0)
  }, [hashtag])

  useEffect(() => {
    loadAll().catch(() => {
      setCfg(null)
      setPosts([])
    })
  }, [loadAll])

  useEffect(() => {
    setIndex((i) => {
      if (posts.length === 0) return 0
      return Math.min(i, posts.length - 1)
    })
  }, [posts])

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(`${apiBaseUrl}/hubs/wall`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection
      .start()
      .then(() => connection.invoke('Subscribe', hashtag))
      .catch(() => {})

    connection.on('ApprovedPostsUpdated', () => {
      loadAll().catch(() => {})
    })

    return () => {
      connection.invoke('Unsubscribe', hashtag).catch(() => {})
      connection.stop().catch(() => {})
    }
  }, [hashtag, loadAll])

  useEffect(() => {
    const id = window.setInterval(() => {
      loadAll().catch(() => {})
    }, 60000)
    return () => window.clearInterval(id)
  }, [loadAll])

  useEffect(() => {
    if (!cfg || posts.length === 0) return

    const iv = window.setInterval(() => {
      setVisible(false)
      window.setTimeout(() => {
        setIndex((i) => (posts.length === 0 ? 0 : (i + 1) % posts.length))
        setVisible(true)
      }, 700)
    }, durationMs)

    return () => window.clearInterval(iv)
  }, [cfg, posts.length, durationMs])

  const current = posts[index]
  const imgUrl = current ? current.mediaUrl ?? current.thumbnailUrl ?? '' : ''

  const themeVars = cfg
    ? ({
        '--hw-bg': cfg.secondaryColor,
        '--hw-primary': cfg.primaryColor,
        '--hw-accent': cfg.accentColor,
      } as CSSProperties)
    : {}

  return (
    <div className="wall-root" style={themeVars}>
      <header className="wall-header">
        <div className="brand-line">
          {cfg?.logoUrl ? <img className="wall-logo" src={cfg.logoUrl} alt="" /> : null}
          <div>
            <h1>{cfg?.title ?? `#${hashtag}`}</h1>
            <p className="hash-line">#{hashtag}</p>
          </div>
        </div>
      </header>

      <main className="wall-main">
        {posts.length === 0 ? (
          <div className="waiting card">
            <p className="waiting-title">Poste sua foto no Instagram usando</p>
            <p className="waiting-tag">#{hashtag}</p>
            <p className="waiting-sub">As fotos aparecem aqui após moderação.</p>
          </div>
        ) : (
          <div className="stage">
            <div className={`frame ${visible ? 'on' : 'off'}`}>
              {imgUrl ? (
                <img src={imgUrl} alt="" className="photo" />
              ) : (
                <div className="empty-img">Sem preview</div>
              )}
              <div className="shine" />
            </div>
            <footer className="caption-bar">
              {cfg?.showCaption !== false && current?.caption ? (
                <p className="caption">{truncate(current.caption, 220)}</p>
              ) : (
                <p className="caption muted"> </p>
              )}
              <div className="qr-row">
                {cfg?.showQrCode !== false ? (
                  <div className="qr-box">
                    <QRCodeSVG value={qrValue} size={140} bgColor="transparent" fgColor="#ffffff" />
                    <span>Poste sua foto com #{hashtag}</span>
                  </div>
                ) : null}
              </div>
            </footer>
          </div>
        )}
      </main>
    </div>
  )
}

function truncate(s: string, max: number) {
  const t = s.trim()
  if (t.length <= max) return t
  return `${t.slice(0, max - 1)}…`
}
