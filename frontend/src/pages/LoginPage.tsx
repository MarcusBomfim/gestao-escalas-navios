import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Logo } from '../components/Logo'

const demoUsers = [
  { label: 'Administrador', email: 'admin.demo@portmanagement.local' },
  { label: 'Planejador', email: 'planner.demo@portmanagement.local' },
  { label: 'Operador', email: 'operator.demo@portmanagement.local' },
  { label: 'Visitante', email: 'viewer.demo@portmanagement.local' },
]

export function LoginPage() {
  const { user, isReady, login, loginToPublicDemo } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('planner.demo@portmanagement.local')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const destination = getDestination(location.state)

  if (isReady && user) {
    return <Navigate to={destination} replace />
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      await login(email, password)
      navigate(destination, { replace: true })
    } catch (caughtError) {
      setError(caughtError instanceof ApiError
        ? caughtError.message
        : 'Não foi possível entrar. Verifique se a API está em execução.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handlePublicDemo = async () => {
    setError(null)
    setIsSubmitting(true)

    try {
      await loginToPublicDemo()
      navigate(destination, { replace: true })
    } catch (caughtError) {
      setError(caughtError instanceof ApiError
        ? caughtError.message
        : 'O acesso demonstrativo não está disponível neste momento.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="login-page">
      <section className="login-context">
        <Link className="back-link" to="/">← Voltar ao início</Link>
        <div>
          <p className="eyebrow">Ambiente demonstrativo</p>
          <h1>Operação conectada começa por um acesso seguro.</h1>
          <p>
            Escolha um perfil fictício e use a senha definida no arquivo
            de ambiente do projeto.
          </p>
        </div>
        <ul className="security-points">
          <li><span>01</span> Token de acesso curto mantido somente em memória</li>
          <li><span>02</span> Renovação em cookie protegido e rotativo</li>
          <li><span>03</span> Permissões verificadas pela API</li>
        </ul>
      </section>

      <main className="login-panel">
        <div className="login-card">
          <Logo />
          <div className="login-heading">
            <span>Acesso à plataforma</span>
            <h2>Entre na sua conta</h2>
            <p>As contas abaixo existem apenas para demonstração.</p>
          </div>

          <button
            className="button primary public-demo-button"
            type="button"
            disabled={isSubmitting}
            onClick={() => void handlePublicDemo()}
          >
            {isSubmitting ? 'Preparando demonstração…' : 'Entrar como visitante'}
          </button>
          <p className="public-demo-help">Acesso imediato e somente leitura, sem senha.</p>

          <div className="login-divider"><span>ou use uma conta técnica</span></div>

          <div className="demo-user-selector" aria-label="Selecionar conta demonstrativa">
            {demoUsers.map((demoUser) => (
              <button
                key={demoUser.email}
                className={email === demoUser.email ? 'selected' : undefined}
                type="button"
                onClick={() => setEmail(demoUser.email)}
              >
                {demoUser.label}
              </button>
            ))}
          </div>

          <form onSubmit={(event) => void handleSubmit(event)}>
            <label>
              E-mail
              <input
                type="email"
                autoComplete="username"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
              />
            </label>
            <label>
              Senha
              <input
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                minLength={12}
                required
              />
            </label>

            <Link className="auth-inline-link" to="/recuperar-senha">
              Esqueci minha senha
            </Link>

            {error && <div className="form-error" role="alert">{error}</div>}

            <button className="button primary submit-button" disabled={isSubmitting} type="submit">
              {isSubmitting ? 'Validando acesso…' : 'Entrar na plataforma'}
            </button>
          </form>

          <p className="login-help">
            A senha é o valor de <code>DEMO_USER_PASSWORD</code> no seu <code>.env</code>.
          </p>
        </div>
      </main>
    </div>
  )
}

function getDestination(state: unknown) {
  if (
    typeof state === 'object' &&
    state !== null &&
    'from' in state &&
    typeof state.from === 'string' &&
    state.from.startsWith('/')
  ) {
    return state.from
  }

  return '/painel'
}
