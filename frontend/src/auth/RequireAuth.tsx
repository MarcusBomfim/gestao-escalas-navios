import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { LoadingScreen } from '../components/LoadingScreen'
import { useAuth } from './AuthContext'

export function RequireAuth() {
  const { user, isReady } = useAuth()
  const location = useLocation()

  if (!isReady) {
    return <LoadingScreen label="Restaurando sessão segura" />
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}
