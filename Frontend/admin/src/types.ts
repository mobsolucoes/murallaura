export interface HashtagConfiguration {
  id: string
  hashtag: string
  instagramBusinessAccountId: string
  metaTokenConfigured: boolean
  pollIntervalMinutes: number
  isMonitoringEnabled: boolean
  createdAt: string
  updatedAt: string
}

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

export interface DashboardStats {
  totalCaptured: number
  approved: number
  pending: number
  rejected: number
}

export interface IntegrationLog {
  id: string
  hashtagConfigurationId: string | null
  level: number
  message: string
  details: string | null
  createdAt: string
}
