import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ApiError, requestPasswordReset } from '../api/client'
import { Logo } from '../components/Logo'

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const response = await requestPasswordReset(email)
      setMessage(response.message)
    } catch (caughtError) {
      setError(caughtError instanceof ApiError
        ? caughtError.message
        : 'Não foi possível concluir a solicitação. Tente novamente.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="login-page">
      <section className="login-context">
        <Link className="back-link" to="/login">← Voltar ao acesso</Link>
        <div>
          <p className="eyebrow">Recuperação de acesso</p>
          <h1>Retome o acesso com segurança.</h1>
          <p>
            Informe seu e-mail. Se houver uma conta ativa, enviaremos um link temporário
            para a redefinição da senha.
          </p>
        </div>
        <ul className="security-points">
          <li><span>01</span> A resposta não revela contas cadastradas</li>
          <li><span>02</span> O link é temporário e de uso controlado</li>
          <li><span>03</span> Sessões anteriores são revogadas após a troca</li>
        </ul>
      </section>

      <main className="login-panel">
        <div className="login-card">
          <Logo />
          <div className="login-heading">
            <span>Recuperar senha</span>
            <h2>Solicite um novo acesso</h2>
            <p>Use o e-mail associado à conta.</p>
          </div>

          {message ? (
            <div className="recovery-result" role="status">
              <strong>Solicitação recebida</strong>
              <p>{message}</p>
              <Link className="button primary submit-button" to="/login">
                Voltar para o login
              </Link>
            </div>
          ) : (
            <form onSubmit={(event) => void handleSubmit(event)}>
              <label>
                E-mail
                <input
                  type="email"
                  autoComplete="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  maxLength={320}
                  required
                />
              </label>

              {error && <div className="form-error" role="alert">{error}</div>}

              <button className="button primary submit-button" disabled={isSubmitting} type="submit">
                {isSubmitting ? 'Enviando instruções…' : 'Enviar instruções'}
              </button>
            </form>
          )}
        </div>
      </main>
    </div>
  )
}
