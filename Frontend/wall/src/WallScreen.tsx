import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { QRCodeSVG } from 'qrcode.react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import type { CSSProperties } from 'react'
import { Link, useParams } from 'react-router-dom'
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

  const qrValue = useMemo(
    () => `${window.location.origin}/wall/${encodeURIComponent(hashtag)}/post`,
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
  }, [hashtag])

  useEffect(() => {
    loadAll().catch(() => {
      setCfg(null)
      setPosts([])
    })
  }, [loadAll])

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
        <div className="ig-top">
          <div className="brand-line">
            {cfg?.logoUrl ? <img className="wall-logo" src={cfg.logoUrl} alt="" /> : null}
            <div>
              <h1>{cfg?.title ?? `#${hashtag}`}</h1>
              <p className="hash-line">#{hashtag}</p>
            </div>
          </div>
          <div className="top-actions">
            <a className="post-link" href={`https://www.instagram.com/explore/tags/${encodeURIComponent(hashtag)}/`} target="_blank" rel="noreferrer">
              Ver no Instagram
            </a>
            <Link className="post-link primary" to={`/wall/${encodeURIComponent(hashtag)}/post`}>
              Postar no mural
            </Link>
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
          <div className="feed-wrap">
            {cfg?.showQrCode !== false ? (
              <div className="feed-cta">
                <div className="qr-box">
                  <QRCodeSVG value={qrValue} size={108} bgColor="transparent" fgColor="#111111" />
                  <span>Poste sua foto/video direto na plataforma</span>
                </div>
              </div>
            ) : null}

            <div className="feed-list">
              {posts.map((post) => {
                const imgUrl = post.mediaUrl ?? post.thumbnailUrl ?? ''
                const isVideo = post.mediaType.toLowerCase().includes('video')

                return (
                  <article key={post.id} className="ig-card">
                    <header className="ig-card-head">
                      <div className="ig-avatar">#</div>
                      <div className="ig-author">
                        <strong>{cfg?.title ?? `#${hashtag}`}</strong>
                        <span>#{hashtag}</span>
                      </div>
                    </header>

                    {imgUrl ? (
                      isVideo ? (
                        <video src={imgUrl} className="ig-media" autoPlay muted loop playsInline controls />
                      ) : (
                        <img src={imgUrl} alt="" className="ig-media" loading="lazy" />
                      )
                    ) : (
                      <div className="empty-img">Sem preview</div>
                    )}

                    <div className="ig-actions" aria-hidden>
                      <span>♡</span>
                      <span>💬</span>
                      <span>➤</span>
                    </div>

                    {cfg?.showCaption !== false && post.caption ? (
                      <p className="ig-caption">
                        <strong>{cfg?.title ?? `#${hashtag}`}</strong> {truncate(post.caption, 240)}
                      </p>
                    ) : null}
                  </article>
                )
              })}
            </div>
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
