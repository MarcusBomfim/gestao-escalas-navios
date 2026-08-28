import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { Logo } from '../components/Logo'
import { useAuth } from '../auth/AuthContext'
import type { SecurityRole } from '../api/types'
import { NotificationCenter } from '../components/NotificationCenter'

const navigation: Array<{ to: string; label: string; code: string; end: boolean; roles?: SecurityRole[] }> = [
  { to: '/painel', label: 'Visão geral', code: 'VG', end: true },
  { to: '/navios', label: 'Navios', code: 'NV', end: false },
  { to: '/escalas', label: 'Escalas', code: 'ES', end: false },
  { to: '/agenda', label: 'Agenda', code: 'AG', end: true },
  { to: '/usuarios', label: 'Usuários', code: 'US', end: true, roles: ['Administrator'] },
  { to: '/cadastros', label: 'Cadastros', code: 'CD', end: true, roles: ['Administrator'] },
  { to: '/auditoria', label: 'Auditoria', code: 'AU', end: true, roles: ['Administrator'] },
  { to: '/observabilidade', label: 'Saúde', code: 'OB', end: true, roles: ['Administrator'] },
]

const pageTitles: Record<string, { eyebrow: string; title: string }> = {
  '/painel': { eyebrow: 'Centro de controle', title: 'Visão geral da operação' },
  '/navios': { eyebrow: 'Cadastro operacional', title: 'Navios' },
  '/escalas': { eyebrow: 'Planejamento portuário', title: 'Escalas' },
  '/navios/novo': { eyebrow: 'Cadastro operacional', title: 'Novo navio' },
  '/escalas/nova': { eyebrow: 'Planejamento portuário', title: 'Nova escala' },
  '/agenda': { eyebrow: 'Planejamento portuário', title: 'Agenda de berços' },
  '/usuarios': { eyebrow: 'Administração de acesso', title: 'Usuários e permissões' },
  '/cadastros': { eyebrow: 'Administração operacional', title: 'Cadastros mestres' },
  '/auditoria': { eyebrow: 'Governança operacional', title: 'Auditoria e relatórios' },
  '/observabilidade': { eyebrow: 'Diagnóstico técnico', title: 'Saúde e observabilidade' },
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
  const visibleNavigation = navigation.filter((item) =>
    !item.roles || item.roles.some((role) => user?.roles.includes(role)))

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="dashboard-shell">
      <a className="skip-link" href="#conteudo-principal">Ir para o conteúdo</a>

      <aside className="sidebar">
        <Logo />

        <div className="sidebar-port">
          <span>Porto demonstrativo</span>
          <strong>BRSSZ</strong>
          <small>Centro de operações</small>
        </div>

        <nav className="primary-navigation" aria-label="Navegação principal">
          <span className="navigation-label">Módulos</span>
          {visibleNavigation.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) => isActive ? 'active' : undefined}
            >
              <NavigationIcon code={item.code} />
              <span>{item.label}</span>
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
            <span className="header-status"><i aria-hidden="true" /> Sistema conectado</span>
            <NotificationCenter />
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
        {visibleNavigation.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className={({ isActive }) => isActive ? 'active' : undefined}
          >
            <NavigationIcon code={item.code} />
            {item.label}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}

function NavigationIcon({ code }: { code: string }) {
  const paths: Record<string, React.ReactNode> = {
    VG: <><rect x="3" y="3" width="7" height="7" rx="1" /><rect x="14" y="3" width="7" height="4" rx="1" /><rect x="14" y="11" width="7" height="10" rx="1" /><rect x="3" y="14" width="7" height="7" rx="1" /></>,
    NV: <><path d="M3 14h18l-3.5 5H6.5L3 14Z" /><path d="M8 11V5h7v6M15 8h3l2 3" /></>,
    ES: <><path d="M5 4h14v16H5z" /><path d="M8 8h8M8 12h8M8 16h5" /></>,
    AG: <><rect x="3" y="5" width="18" height="16" rx="2" /><path d="M7 3v4M17 3v4M3 10h18M8 14h2M14 14h2M8 18h2" /></>,
    US: <><circle cx="9" cy="8" r="3" /><path d="M3.5 20v-2.5A4.5 4.5 0 0 1 8 13h2a4.5 4.5 0 0 1 4.5 4.5V20M17 8h4M19 6v4" /></>,
    CD: <><rect x="4" y="4" width="16" height="16" rx="2" /><path d="M8 8h8M8 12h8M8 16h5" /></>,
    AU: <><path d="M4 4h16v16H4zM8 9h8M8 13h5M8 17h3" /><path d="m15 16 1.5 1.5L20 14" /></>,
    OB: <><path d="M3 12h4l2-5 4 10 2-5h6" /><circle cx="12" cy="12" r="10" /></>,
  }

  return (
    <svg className="navigation-icon" viewBox="0 0 24 24" aria-hidden="true">
      {paths[code]}
    </svg>
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
