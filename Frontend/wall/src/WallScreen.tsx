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
        '--hw-primary': cfg.primaryColor,
        '--hw-accent': cfg.accentColor,
      } as CSSProperties)
    : {}

  return (
    <div className="wall-root" style={themeVars}>
      <main className="wall-main">
        <div className="feed-wrap">
          {cfg?.showQrCode !== false ? (
            <div className="feed-cta">
              <div className="qr-box">
                <QRCodeSVG value={qrValue} size={108} bgColor="transparent" fgColor="#111111" />
                <span>Poste sua foto/video direto na plataforma</span>
              </div>
            </div>
          ) : null}

          {posts.length === 0 ? (
            <div className="waiting card">
              <p className="waiting-title">Poste sua foto no Instagram usando</p>
              <p className="waiting-tag">#{hashtag}</p>
              <p className="waiting-sub">As fotos aparecem aqui após moderação.</p>
            </div>
          ) : (
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
                      <HeartIcon />
                      <CommentIcon />
                      <ShareIcon />
                      <span className="ig-actions-spacer" />
                      <SaveIcon />
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
          )}
        </div>
      </main>
      <nav className="ig-mobile-nav" aria-hidden>
        <HomeIcon />
        <SearchIcon />
        <ReelsIcon />
        <ShopIcon />
        <ProfileIcon />
      </nav>
    </div>
  )
}

function truncate(s: string, max: number) {
  const t = s.trim()
  if (t.length <= max) return t
  return `${t.slice(0, max - 1)}…`
}

function HeartIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden>
      <path d="M12.1 20.81 12 20.9l-.11-.09C7.14 16.75 4 13.89 4 10.5 4 7.91 5.99 6 8.5 6c1.74 0 3.41.81 4.5 2.09C14.09 6.81 15.76 6 17.5 6 20.01 6 22 7.91 22 10.5c0 3.39-3.14 6.25-7.9 10.31z" />
    </svg>
  )
}

function CommentIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden>
      <path d="M21 6.5A3.5 3.5 0 0 0 17.5 3h-11A3.5 3.5 0 0 0 3 6.5v7A3.5 3.5 0 0 0 6.5 17H8v4l4.29-4H17.5A3.5 3.5 0 0 0 21 13.5v-7z" />
    </svg>
  )
}

function ShareIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden>
      <path d="M22 2 11 13" />
      <path d="M22 2 15 22l-4-9-9-4z" />
    </svg>
  )
}

function SaveIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden>
      <path d="M6 3h12a1 1 0 0 1 1 1v17l-7-4-7 4V4a1 1 0 0 1 1-1z" />
    </svg>
  )
}

function HomeIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden>
      <path d="M3 11.5 12 4l9 7.5" />
      <path d="M5 10.5V20h14v-9.5" />
    </svg>
  )
}

function SearchIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden>
      <circle cx="11" cy="11" r="7" />
      <path d="m20 20-3.5-3.5" />
    </svg>
  )
}

function ReelsIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden>
      <rect x="4" y="4" width="16" height="16" rx="3" />
      <path d="m10 9 5 3-5 3z" />
      <path d="M4 9h16" />
    </svg>
  )
}

function ShopIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden>
      <path d="M6 7h12l-1 13H7L6 7z" />
      <path d="M9 7V5a3 3 0 0 1 6 0v2" />
    </svg>
  )
}

function ProfileIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden>
      <circle cx="12" cy="8" r="3.2" />
      <path d="M5.5 20a6.5 6.5 0 0 1 13 0" />
    </svg>
  )
}
