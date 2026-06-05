import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { auth, getToken, logout as clearToken } from '../api'
import { normalizeUser, type UserProfile } from '../types/user'

type AuthContextValue = {
  isAuthenticated: boolean
  user: UserProfile | null
  userLoading: boolean
  onAuthSuccess: () => void
  logout: () => void
  refreshUser: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

/** Provides authentication state for the app shell and auth overlay. */
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(() => !!getToken())
  const [user, setUser] = useState<UserProfile | null>(null)
  const [userLoading, setUserLoading] = useState(false)

  const refreshUser = useCallback(async () => {
    if (!getToken()) {
      setUser(null)
      return
    }
    setUserLoading(true)
    try {
      const raw = await auth.me()
      setUser(normalizeUser(raw ?? {}))
    } catch {
      setUser(null)
    } finally {
      setUserLoading(false)
    }
  }, [])

  useEffect(() => {
    if (isAuthenticated) {
      refreshUser()
    } else {
      setUser(null)
    }
  }, [isAuthenticated, refreshUser])

  const onAuthSuccess = useCallback(() => setIsAuthenticated(true), [])

  const logout = useCallback(() => {
    clearToken()
    setUser(null)
    setIsAuthenticated(false)
  }, [])

  const value = useMemo(
    () => ({ isAuthenticated, user, userLoading, onAuthSuccess, logout, refreshUser }),
    [isAuthenticated, user, userLoading, onAuthSuccess, logout, refreshUser]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

/** Returns the current authentication context. */
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
