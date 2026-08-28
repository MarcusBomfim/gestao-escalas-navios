import { createContext, useContext } from 'react'
import type { AuthenticatedUser, SecurityRole } from '../api/types'

export interface AuthContextValue {
  user: AuthenticatedUser | null
  isReady: boolean
  login: (email: string, password: string) => Promise<void>
  loginToPublicDemo: () => Promise<void>
  logout: () => Promise<void>
  hasAnyRole: (...roles: SecurityRole[]) => boolean
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth deve ser utilizado dentro de AuthProvider.')
  }
  return context
}
