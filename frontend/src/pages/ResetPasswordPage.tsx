import { useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ApiError, resetPassword } from '../api/client'
import { Logo } from '../components/Logo'

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const userId = searchParams.get('user') ?? ''
  const token = searchParams.get('token') ?? ''
  const [password, setPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [isComplete, setIsComplete] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const hasValidParameters = userId.length > 0 && token.length > 0

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    if (password !== confirmation) {
      setError('As senhas informadas não são iguais.')
      return
    }

    setIsSubmitting(true)
    try {
      await resetPassword(userId, token, password)
      setIsComplete(true)
    } catch (caughtError) {
      setError(caughtError instanceof ApiError
        ? caughtError.message
        : 'Não foi possível redefinir a senha. Solicite um novo link.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="login-page">
      <section className="login-context">
        <Link className="back-link" to="/login">← Voltar ao acesso</Link>
        <div>
          <p className="eyebrow">Nova credencial</p>
          <h1>Defina uma senha forte.</h1>
          <p>
            A nova senha substitui a anterior e encerra as outras sessões ativas da conta.
          </p>
        </div>
        <ul className="security-points">
          <li><span>01</span> Mínimo de 12 caracteres</li>
          <li><span>02</span> Maiúscula, minúscula, número e símbolo</li>
          <li><span>03</span> Token temporário validado no servidor</li>
        </ul>
      </section>

      <main className="login-panel">
        <div className="login-card">
          <Logo />
          <div className="login-heading">
            <span>Redefinir senha</span>
            <h2>{isComplete ? 'Senha atualizada' : 'Crie sua nova senha'}</h2>
            <p>{isComplete
              ? 'Agora você pode entrar com a nova credencial.'
              : 'A senha deve cumprir todos os requisitos de segurança.'}</p>
          </div>

          {!hasValidParameters ? (
            <div className="recovery-result" role="alert">
              <strong>Link incompleto</strong>
              <p>Solicite um novo link de recuperação para continuar.</p>
              <Link className="button primary submit-button" to="/recuperar-senha">
                Solicitar novo link
              </Link>
            </div>
          ) : isComplete ? (
            <div className="recovery-result" role="status">
              <strong>Redefinição concluída</strong>
              <p>As sessões anteriores foram encerradas para proteger sua conta.</p>
              <Link className="button primary submit-button" to="/login">
                Entrar na plataforma
              </Link>
            </div>
          ) : (
            <form onSubmit={(event) => void handleSubmit(event)}>
              <label>
                Nova senha
                <input
                  type="password"
                  autoComplete="new-password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  minLength={12}
                  maxLength={256}
                  required
                />
              </label>
              <label>
                Confirmar nova senha
                <input
                  type="password"
                  autoComplete="new-password"
                  value={confirmation}
                  onChange={(event) => setConfirmation(event.target.value)}
                  minLength={12}
                  maxLength={256}
                  required
                />
              </label>

              <p className="password-requirements">
                Use ao menos 12 caracteres, incluindo maiúscula, minúscula, número e símbolo.
              </p>

              {error && <div className="form-error" role="alert">{error}</div>}

              <button className="button primary submit-button" disabled={isSubmitting} type="submit">
                {isSubmitting ? 'Atualizando senha…' : 'Redefinir senha'}
              </button>
            </form>
          )}
        </div>
      </main>
    </div>
  )
}
