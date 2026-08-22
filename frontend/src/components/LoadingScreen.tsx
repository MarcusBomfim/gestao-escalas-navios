export function LoadingScreen({ label = 'Carregando' }: { label?: string }) {
  return (
    <div className="loading-screen" role="status" aria-live="polite">
      <span className="loading-mark" aria-hidden="true">GE</span>
      <span className="loading-spinner" aria-hidden="true" />
      <p>{label}</p>
    </div>
  )
}
