export interface WallConfiguration {
  id: string
  hashtagConfigurationId: string
  title: string
  logoUrl: string | null
  primaryColor: string
  secondaryColor: string
  accentColor: string
  theme: string
  displayDurationSeconds: number
  showCaption: boolean
  showQrCode: boolean
}

export interface InstagramMediaPost {
  id: string
  instagramMediaId: string
  hashtag: string
  caption: string | null
  mediaUrl: string | null
  thumbnailUrl: string | null
  permalink: string
  mediaType: string
  timestamp: string
  status: number
  createdAt: string
}
