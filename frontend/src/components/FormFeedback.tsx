import { ApiError } from '../api/client'

export function FormError({ error }: { error: unknown }) {
  if (!error) {
    return null
  }

  const message = error instanceof ApiError
    ? error.message
    : 'Não foi possível concluir a operação. Tente novamente.'

  return <div className="form-feedback error" role="alert">{message}</div>
}
