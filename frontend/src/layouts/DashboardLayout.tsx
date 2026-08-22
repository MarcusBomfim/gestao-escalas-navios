import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { Logo } from '../components/Logo'
import { useAuth } from '../auth/AuthContext'
import type { SecurityRole } from '../api/types'

const navigation = [
  { to: '/painel', label: 'Visão geral', code: 'VG', end: true },
  { to: '/navios', label: 'Navios', code: 'NV', end: false },
  { to: '/escalas', label: 'Escalas', code: 'ES', end: false },
  { to: '/agenda', label: 'Agenda', code: 'AG', end: true },
]

const pageTitles: Record<string, { eyebrow: string; title: string }> = {
  '/painel': { eyebrow: 'Centro de controle', title: 'Visão geral da operação' },
  '/navios': { eyebrow: 'Cadastro operacional', title: 'Navios' },
  '/escalas': { eyebrow: 'Planejamento portuário', title: 'Escalas' },
  '/navios/novo': { eyebrow: 'Cadastro operacional', title: 'Novo navio' },
  '/escalas/nova': { eyebrow: 'Planejamento portuário', title: 'Nova escala' },
  '/agenda': { eyebrow: 'Planejamento portuário', title: 'Agenda de berços' },
}

const roleLabels: Record<SecurityRole, string> = {
  Administrator: 'Administrador',
  Planner: 'Planejador',
  Operator: 'Operador',
  Viewer: 'Visitante',
}

export function DashboardLayout() {
  const { user, logout } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const page = getPageTitle(location.pathname)
  const primaryRole = user?.roles[0]

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="dashboard-shell">
      <a className="skip-link" href="#conteudo-principal">Ir para o conteúdo</a>

      <aside className="sidebar">
        <Logo />

        <nav className="primary-navigation" aria-label="Navegação principal">
          <span className="navigation-label">Operação</span>
          {navigation.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) => isActive ? 'active' : undefined}
            >
              <span aria-hidden="true">{item.code}</span>
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-context">
          <span>Ambiente</span>
          <strong><i aria-hidden="true" /> Demonstração segura</strong>
          <small>Somente dados sintéticos</small>
        </div>
      </aside>

      <div className="dashboard-workspace">
        <header className="dashboard-header">
          <div>
            <p>{page.eyebrow}</p>
            <h1>{page.title}</h1>
          </div>

          <div className="user-menu">
            <span className="user-avatar" aria-hidden="true">
              {getInitials(user?.displayName ?? 'Usuário')}
            </span>
            <span className="user-identity">
              <strong>{user?.displayName}</strong>
              <small>{primaryRole ? roleLabels[primaryRole] : 'Usuário'}</small>
            </span>
            <button type="button" onClick={() => void handleLogout()}>Sair</button>
          </div>
        </header>

        <main id="conteudo-principal" className="dashboard-content">
          <Outlet />
        </main>
      </div>

      <nav className="mobile-navigation" aria-label="Navegação móvel">
        {navigation.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className={({ isActive }) => isActive ? 'active' : undefined}
          >
            <span aria-hidden="true">{item.code}</span>
            {item.label}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}

function getInitials(name: string) {
  return name
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part.charAt(0))
    .join('')
    .toUpperCase()
}

function getPageTitle(pathname: string) {
  if (pageTitles[pathname]) {
    return pageTitles[pathname]
  }
  if (/^\/navios\/[^/]+\/editar$/.test(pathname)) {
    return { eyebrow: 'Cadastro operacional', title: 'Editar navio' }
  }
  if (/^\/escalas\/[^/]+$/.test(pathname)) {
    return { eyebrow: 'Planejamento portuário', title: 'Detalhes da escala' }
  }
  return pageTitles['/painel']!
}
