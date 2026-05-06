import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { api } from '../api/client'

interface AuthState {
  token: string | null
  username: string | null
}

const AuthContext = createContext<{
  auth: AuthState
  login: (username: string, password: string) => Promise<void>
  logout: () => void
} | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('hw_token'))
  const [username, setUsername] = useState<string | null>(() => localStorage.getItem('hw_user') ?? null)

  const login = useCallback(async (user: string, password: string) => {
    const { data } = await api.post<{ token: string }>('/api/auth/login', { username: user, password })
    localStorage.setItem('hw_token', data.token)
    localStorage.setItem('hw_user', user)
    setToken(data.token)
    setUsername(user)
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem('hw_token')
    localStorage.removeItem('hw_user')
    setToken(null)
    setUsername(null)
  }, [])

  const value = useMemo(
    () => ({
      auth: { token, username },
      login,
      logout,
    }),
    [token, username, login, logout]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('AuthProvider missing')
  return ctx
}
