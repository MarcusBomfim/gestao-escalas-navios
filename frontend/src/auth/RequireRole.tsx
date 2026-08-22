import { Navigate, Outlet } from 'react-router-dom'
import type { SecurityRole } from '../api/types'
import { useAuth } from './AuthContext'

export function RequireRole({ roles }: { roles: SecurityRole[] }) {
  const { hasAnyRole } = useAuth()

  return hasAnyRole(...roles)
    ? <Outlet />
    : <Navigate to="/painel" replace />
}
