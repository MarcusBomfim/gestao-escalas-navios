import { environment } from '../config/environment'
import './app.css'

const modules = [
  {
    number: '01',
    title: 'Escalas',
    description: 'Planejamento e acompanhamento do ciclo completo do navio.',
  },
  {
    number: '02',
    title: 'Berços',
    description: 'Janelas de atracação, compatibilidade e prevenção de conflitos.',
  },
  {
    number: '03',
    title: 'Operações',
    description: 'Fundeio, atracação, movimentação de carga e desatracação.',
  },
  {
    number: '04',
    title: 'Auditoria',
    description: 'Histórico rastreável das decisões e alterações operacionais.',
  },
]

const milestones = [
  { label: 'Solicitação recebida', time: '08:10', active: true },
  { label: 'Análise operacional', time: '08:32', active: true },
  { label: 'Janela de berço', time: '10:45', active: false },
  { label: 'Operação portuária', time: 'A programar', active: false },
]

export function App() {
  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="#inicio" aria-label="Ir para o início">
          <span className="brand-mark" aria-hidden="true">GE</span>
          <span>
            <strong>Gestão de Escalas</strong>
            <small>Operações portuárias</small>
          </span>
        </a>

        <span className="environment-status">
          <i aria-hidden="true" /> Estrutura inicial
        </span>
      </header>

      <main id="inicio">
        <section className="hero-section">
          <div className="hero-copy">
            <p className="eyebrow">Operação portuária · Parte 2</p>
            <h1>Uma base confiável para cada escala.</h1>
            <p className="hero-description">
              Estrutura preparada para centralizar navios, berços, horários e
              eventos operacionais com segurança e rastreabilidade.
            </p>

            <dl className="technology-list" aria-label="Tecnologias da aplicação">
              <div>
                <dt>Back-end</dt>
                <dd>ASP.NET Core</dd>
              </div>
              <div>
                <dt>Interface</dt>
                <dd>React + TypeScript</dd>
              </div>
              <div>
                <dt>Dados</dt>
                <dd>PostgreSQL</dd>
              </div>
            </dl>
          </div>

          <aside className="operation-card" aria-label="Exemplo de linha do tempo">
            <div className="operation-card-header">
              <div>
                <span>Escala demonstrativa</span>
                <strong>BRSSZ · 2026-0042</strong>
              </div>
              <span className="planned-badge">Em análise</span>
            </div>

            <ol className="milestone-list">
              {milestones.map((milestone) => (
                <li className={milestone.active ? 'completed' : ''} key={milestone.label}>
                  <span aria-hidden="true" />
                  <div>
                    <strong>{milestone.label}</strong>
                    <small>{milestone.time}</small>
                  </div>
                </li>
              ))}
            </ol>
          </aside>
        </section>

        <section className="modules-section" aria-labelledby="modules-title">
          <div className="section-heading">
            <p className="eyebrow">Módulos do domínio</p>
            <h2 id="modules-title">Fronteiras claras desde o início.</h2>
          </div>

          <div className="modules-grid">
            {modules.map((module) => (
              <article key={module.number}>
                <span>{module.number}</span>
                <h3>{module.title}</h3>
                <p>{module.description}</p>
              </article>
            ))}
          </div>
        </section>
      </main>

      <footer>
        <span>Projeto demonstrativo com dados sintéticos.</span>
        <span className="api-reference">API: {environment.apiUrl}</span>
      </footer>
    </div>
  )
}

