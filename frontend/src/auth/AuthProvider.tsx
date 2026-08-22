import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { restoreSession, signIn, signOut, subscribeToSession } from '../api/client'
import type { SecurityRole, SessionResponse } from '../api/types'
import { AuthContext, type AuthContextValue } from './AuthContext'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<SessionResponse | null>(null)
  const [isReady, setIsReady] = useState(false)

  useEffect(() => {
    let isActive = true
    const unsubscribe = subscribeToSession((nextSession) => {
      if (isActive) {
        setSession(nextSession)
      }
    })

    void restoreSession()
      .catch(() => undefined)
      .finally(() => {
        if (isActive) {
          setIsReady(true)
        }
      })

    return () => {
      isActive = false
      unsubscribe()
    }
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const nextSession = await signIn(email, password)
    setSession(nextSession)
  }, [])

  const logout = useCallback(async () => {
    await signOut()
    setSession(null)
  }, [])

  const hasAnyRole = useCallback(
    (...roles: SecurityRole[]) =>
      session?.user.roles.some((role) => roles.includes(role)) ?? false,
    [session],
  )

  const value = useMemo<AuthContextValue>(
    () => ({ user: session?.user ?? null, isReady, login, logout, hasAnyRole }),
    [hasAnyRole, isReady, login, logout, session],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
