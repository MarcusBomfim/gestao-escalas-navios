import { Link } from 'react-router-dom'
import { Logo } from '../components/Logo'

const capabilities = [
  {
    number: '01',
    title: 'Escalas',
    description: 'Acompanhamento do ciclo do navio, do pedido ao encerramento.',
  },
  {
    number: '02',
    title: 'Berços',
    description: 'Estrutura portuária, compatibilidade física e planejamento.',
  },
  {
    number: '03',
    title: 'Operação',
    description: 'Situações operacionais registradas com histórico rastreável.',
  },
]

const timeline = [
  { label: 'Solicitação recebida', time: '08:10', state: 'complete' },
  { label: 'Análise operacional', time: '08:32', state: 'complete' },
  { label: 'Janela de berço', time: '10:45', state: 'current' },
  { label: 'Operação portuária', time: 'A programar', state: 'future' },
]

export function LandingPage() {
  return (
    <div className="public-shell">
      <header className="public-header">
        <Logo />
        <nav aria-label="Acesso principal">
          <a href="#recursos">Recursos</a>
          <Link className="button secondary" to="/login">Entrar</Link>
        </nav>
      </header>

      <div className="operational-strip" aria-label="Estado do ambiente demonstrativo">
        <span><i aria-hidden="true" /> Ambiente operacional disponível</span>
        <span>BRSSZ</span>
        <span>UTC−03:00</span>
        <span>Dados simulados</span>
      </div>

      <main>
        <section className="landing-hero">
          <div className="landing-copy">
            <div className="landing-index"><span>PORT CONTROL</span><strong>01 / VISÃO GERAL</strong></div>
            <p className="eyebrow">Gestão portuária integrada</p>
            <h1>Clareza operacional para cada escala.</h1>
            <p>
              Uma plataforma demonstrativa para organizar navios, escalas,
              terminais e decisões operacionais com segurança e rastreabilidade.
            </p>
            <div className="hero-actions">
              <Link className="button primary" to="/login">Acessar plataforma</Link>
              <a className="text-link" href="#recursos">Conhecer recursos <span>→</span></a>
            </div>
            <dl className="landing-metrics">
              <div><dt>04</dt><dd>Perfis de acesso</dd></div>
              <div><dt>120</dt><dd>Testes automatizados</dd></div>
              <div><dt>100%</dt><dd>Dados sintéticos</dd></div>
            </dl>
          </div>

          <aside className="operation-card" aria-label="Exemplo de evolução de uma escala">
            <div className="operation-card-header">
              <div>
                <span>Painel de acompanhamento</span>
                <strong>BRSSZ · DEMO-002</strong>
              </div>
              <span className="planned-badge">Em análise</span>
            </div>
            <ol className="milestone-list">
              {timeline.map((item) => (
                <li className={item.state} key={item.label}>
                  <span aria-hidden="true" />
                  <div><strong>{item.label}</strong><small>{item.time}</small></div>
                </li>
              ))}
            </ol>
            <div className="operation-footer">
              <span>Atualização registrada</span>
              <strong>Agora</strong>
            </div>
          </aside>
        </section>

        <section id="recursos" className="capabilities-section" aria-labelledby="capabilities-title">
          <div className="section-heading">
            <p className="eyebrow">Visão do produto</p>
            <h2 id="capabilities-title">Informação certa, no momento certo.</h2>
          </div>
          <div className="capabilities-grid">
            {capabilities.map((capability) => (
              <article key={capability.number}>
                <span>{capability.number}</span>
                <h3>{capability.title}</h3>
                <p>{capability.description}</p>
              </article>
            ))}
          </div>
        </section>
      </main>

      <footer className="public-footer">
        <Logo compact />
        <p>Projeto demonstrativo · Dados fictícios · 2026</p>
      </footer>
    </div>
  )
}
