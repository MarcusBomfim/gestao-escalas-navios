import { useState, type FormEvent, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  createManagedBerth,
  createManagedOrganization,
  createManagedPort,
  createManagedTerminal,
  listManagedOrganizations,
  listManagedPortStructure,
  updateManagedBerth,
  updateManagedOrganization,
  updateManagedPort,
  updateManagedTerminal,
} from '../api/portManagement'
import type {
  ManagedBerth,
  ManagedBerthStatus,
  ManagedOrganization,
  ManagedPort,
  ManagedTerminal,
  ManagedVesselType,
  OrganizationType,
} from '../api/types'
import { FormError } from '../components/FormFeedback'
import { Pagination } from '../components/Pagination'
import { EmptyState, QueryError, TableSkeleton } from '../components/QueryFeedback'

const organizationTypes: Array<{ value: OrganizationType; label: string }> = [
  { value: 'PortAuthority', label: 'Autoridade portuária' },
  { value: 'TerminalOperator', label: 'Operador de terminal' },
  { value: 'PortOperator', label: 'Operador portuário' },
  { value: 'ShippingLine', label: 'Armador' },
  { value: 'ShippingAgency', label: 'Agência marítima' },
]

const vesselTypes: Array<{ value: ManagedVesselType; label: string }> = [
  { value: 'ContainerShip', label: 'Porta-contêiner' },
  { value: 'BulkCarrier', label: 'Graneleiro' },
  { value: 'Tanker', label: 'Navio-tanque' },
  { value: 'GeneralCargo', label: 'Carga geral' },
  { value: 'RoRo', label: 'Ro-Ro' },
  { value: 'Passenger', label: 'Passageiros' },
  { value: 'Offshore', label: 'Offshore' },
  { value: 'Other', label: 'Outro' },
]

const berthStatusLabels: Record<ManagedBerthStatus, string> = {
  Available: 'Disponível',
  Unavailable: 'Indisponível',
  Maintenance: 'Em manutenção',
}

type Section = 'organizations' | 'ports'
type ActiveFilter = 'all' | 'active' | 'inactive'
type Editor =
  | { kind: 'organization'; target?: ManagedOrganization }
  | { kind: 'port'; target?: ManagedPort }
  | { kind: 'terminal'; portId: string; target?: ManagedTerminal }
  | { kind: 'berth'; terminalId: string; target?: ManagedBerth }

export function MasterDataPage() {
  const [section, setSection] = useState<Section>('organizations')
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [type, setType] = useState<OrganizationType | ''>('')
  const [activeFilter, setActiveFilter] = useState<ActiveFilter>('all')
  const [editor, setEditor] = useState<Editor | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const organizations = useQuery({
    queryKey: ['master-organizations', { page, search, type, activeFilter }],
    queryFn: () => listManagedOrganizations({
      page,
      pageSize: 10,
      search,
      ...(type ? { type } : {}),
      ...(activeFilter === 'all' ? {} : { isActive: activeFilter === 'active' }),
    }),
  })
  const ports = useQuery({
    queryKey: ['master-ports'],
    queryFn: listManagedPortStructure,
  })

  const selectSection = (nextSection: Section) => {
    setSection(nextSection)
    setEditor(null)
    setSuccessMessage(null)
  }

  const openEditor = (nextEditor: Editor) => {
    setEditor(nextEditor)
    setSuccessMessage(null)
    window.requestAnimationFrame(() => {
      document.querySelector('.master-data-editor')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
    })
  }

  const handleSaved = (message: string) => {
    setEditor(null)
    setSuccessMessage(message)
  }

  const handleSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSearch(searchInput.trim())
    setPage(1)
  }

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setType('')
    setActiveFilter('all')
    setPage(1)
  }

  return (
    <div className="page-stack">
      <section className="page-introduction">
        <div>
          <p>Cadastros mestres</p>
          <h2>Estrutura organizacional e portuária</h2>
          <span>Administre referências usadas no planejamento sem apagar o histórico operacional.</span>
        </div>
        <button
          className="primary-action"
          type="button"
          onClick={() => openEditor(section === 'organizations' ? { kind: 'organization' } : { kind: 'port' })}
        >
          {section === 'organizations' ? 'Cadastrar organização' : 'Cadastrar porto'}
        </button>
      </section>

      <nav className="master-data-tabs" aria-label="Áreas dos cadastros mestres" role="tablist">
        <button
          type="button"
          role="tab"
          aria-selected={section === 'organizations'}
          className={section === 'organizations' ? 'active' : undefined}
          onClick={() => selectSection('organizations')}
        >
          Organizações
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={section === 'ports'}
          className={section === 'ports' ? 'active' : undefined}
          onClick={() => selectSection('ports')}
        >
          Portos, terminais e berços
        </button>
      </nav>

      {successMessage && <div className="form-feedback success" role="status">{successMessage}</div>}

      {editor?.kind === 'organization' && (
        <OrganizationEditor target={editor.target} onCancel={() => setEditor(null)} onSaved={handleSaved} />
      )}
      {editor?.kind === 'port' && (
        <PortEditor target={editor.target} onCancel={() => setEditor(null)} onSaved={handleSaved} />
      )}
      {editor?.kind === 'terminal' && (
        <TerminalEditor
          portId={editor.portId}
          target={editor.target}
          onCancel={() => setEditor(null)}
          onSaved={handleSaved}
        />
      )}
      {editor?.kind === 'berth' && (
        <BerthEditor
          terminalId={editor.terminalId}
          target={editor.target}
          onCancel={() => setEditor(null)}
          onSaved={handleSaved}
        />
      )}

      {section === 'organizations' ? (
        <section className="content-card data-card">
          <header className="data-toolbar user-filters">
            <form className="search-form" onSubmit={handleSearch}>
              <label className="sr-only" htmlFor="organization-search">Buscar organização</label>
              <input
                id="organization-search"
                type="search"
                placeholder="Nome ou registro…"
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
              />
              <button type="submit">Buscar</button>
            </form>
            <label className="select-filter">
              <span className="sr-only">Filtrar por tipo</span>
              <select value={type} onChange={(event) => { setType(event.target.value as OrganizationType | ''); setPage(1) }}>
                <option value="">Todos os tipos</option>
                {organizationTypes.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
              </select>
            </label>
            <label className="select-filter">
              <span className="sr-only">Filtrar por situação</span>
              <select value={activeFilter} onChange={(event) => { setActiveFilter(event.target.value as ActiveFilter); setPage(1) }}>
                <option value="all">Todas as situações</option>
                <option value="active">Ativas</option>
                <option value="inactive">Inativas</option>
              </select>
            </label>
            {(search || type || activeFilter !== 'all') && (
              <button className="clear-filter" type="button" onClick={clearFilters}>Limpar filtros</button>
            )}
          </header>

          {organizations.isPending && <TableSkeleton rows={6} />}
          {organizations.isError && <QueryError error={organizations.error} />}
          {organizations.data?.items.length === 0 && (
            <EmptyState title="Nenhuma organização encontrada" description="Altere os filtros ou cadastre uma organização." />
          )}
          {organizations.data && organizations.data.items.length > 0 && (
            <div className="table-scroll">
              <table>
                <thead>
                  <tr><th>Organização</th><th>Registro</th><th>Tipo</th><th>Atualização</th><th>Situação</th><th><span className="sr-only">Ações</span></th></tr>
                </thead>
                <tbody>
                  {organizations.data.items.map((organization) => (
                    <tr key={organization.id}>
                      <td><strong>{organization.name}</strong></td>
                      <td><code className="route-code">{organization.registrationNumber}</code></td>
                      <td>{organizationTypes.find((item) => item.value === organization.type)?.label}</td>
                      <td>{formatDate(organization.updatedAtUtc)}</td>
                      <td><State active={organization.isActive} /></td>
                      <td className="table-action"><button type="button" onClick={() => openEditor({ kind: 'organization', target: organization })}>Editar</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <Pagination page={page} totalPages={organizations.data?.totalPages ?? 0} onChange={setPage} />
        </section>
      ) : (
        <section className="master-port-list">
          {ports.isPending && <section className="content-card"><TableSkeleton rows={7} /></section>}
          {ports.isError && <section className="content-card"><QueryError error={ports.error} /></section>}
          {ports.data?.length === 0 && (
            <section className="content-card"><EmptyState title="Nenhum porto cadastrado" description="Cadastre o primeiro porto da estrutura." /></section>
          )}
          {ports.data?.map((port) => (
            <PortStructureCard key={port.id} port={port} onEdit={openEditor} />
          ))}
        </section>
      )}
    </div>
  )
}

function PortStructureCard({ port, onEdit }: { port: ManagedPort; onEdit: (editor: Editor) => void }) {
  return (
    <article className="content-card master-port-card">
      <header>
        <div className="master-code">{port.unLocode}</div>
        <div>
          <span>{port.countryCode} · {port.timeZoneId}</span>
          <h3>{port.name}</h3>
          <small>{port.terminals.length} terminais · {port.terminals.reduce((total, terminal) => total + terminal.berths.length, 0)} berços</small>
        </div>
        <State active={port.isActive} />
        <div className="master-actions">
          <button type="button" onClick={() => onEdit({ kind: 'port', target: port })}>Editar porto</button>
          <button type="button" disabled={!port.isActive} onClick={() => onEdit({ kind: 'terminal', portId: port.id })}>Novo terminal</button>
        </div>
      </header>

      <div className="master-terminal-list">
        {port.terminals.length === 0 && <p className="master-empty">Nenhum terminal vinculado.</p>}
        {port.terminals.map((terminal) => (
          <article className="master-terminal" key={terminal.id}>
            <header>
              <div>
                <span>{terminal.code}</span>
                <h4>{terminal.name}</h4>
                <small>{terminal.timeZoneId}</small>
              </div>
              <State active={terminal.isActive} />
              <div className="master-actions">
                <button type="button" onClick={() => onEdit({ kind: 'terminal', portId: port.id, target: terminal })}>Editar</button>
                <button type="button" disabled={!terminal.isActive} onClick={() => onEdit({ kind: 'berth', terminalId: terminal.id })}>Novo berço</button>
              </div>
            </header>

            <div className="master-berth-grid">
              {terminal.berths.length === 0 && <p className="master-empty">Nenhum berço cadastrado.</p>}
              {terminal.berths.map((berth) => (
                <button
                  className="master-berth"
                  type="button"
                  key={berth.id}
                  onClick={() => onEdit({ kind: 'berth', terminalId: terminal.id, target: berth })}
                >
                  <span>{berth.code}</span>
                  <strong>{berth.name}</strong>
                  <small>{berth.usefulLengthMeters} m × {berth.maximumBeamMeters} m · calado {berth.maximumDraftMeters} m</small>
                  <em className={berth.status.toLowerCase()}>{berthStatusLabels[berth.status]}</em>
                </button>
              ))}
            </div>
          </article>
        ))}
      </div>
    </article>
  )
}

interface EditorProps {
  onCancel: () => void
  onSaved: (message: string) => void
}

function OrganizationEditor({ target, onCancel, onSaved }: EditorProps & { target?: ManagedOrganization | undefined }) {
  const queryClient = useQueryClient()
  const mutation = useMutation({
    mutationFn: (input: { name: string; registrationNumber: string; type: OrganizationType; isActive: boolean }) =>
      target
        ? updateManagedOrganization(target.id, { ...input, expectedUpdatedAtUtc: target.updatedAtUtc })
        : createManagedOrganization(input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['master-organizations'] })
      await queryClient.invalidateQueries({ queryKey: ['user-management-options'] })
      onSaved(target ? 'Organização atualizada com sucesso.' : 'Organização cadastrada com sucesso.')
    },
  })

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    mutation.mutate({
      name: String(data.get('name') ?? '').trim(),
      registrationNumber: String(data.get('registrationNumber') ?? '').trim(),
      type: String(data.get('type')) as OrganizationType,
      isActive: target ? String(data.get('isActive')) === 'true' : true,
    })
  }

  return (
    <EditorShell code="OR" title={target ? 'Editar organização' : 'Cadastrar organização'} description="O registro deve ser único e a desativação preserva todo o histórico." pending={mutation.isPending} error={mutation.error} onCancel={onCancel} onSubmit={submit} submitLabel={target ? 'Salvar alterações' : 'Cadastrar organização'}>
      <label className="field">Nome<input name="name" required maxLength={180} defaultValue={target?.name} /></label>
      <label className="field">Registro<input name="registrationNumber" required maxLength={40} defaultValue={target?.registrationNumber} /></label>
      <label className="field">Tipo<select name="type" defaultValue={target?.type ?? 'ShippingAgency'}>{organizationTypes.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label>
      {target && <label className="field">Situação<select name="isActive" defaultValue={String(target.isActive)}><option value="true">Ativa</option><option value="false">Inativa</option></select></label>}
    </EditorShell>
  )
}

function PortEditor({ target, onCancel, onSaved }: EditorProps & { target?: ManagedPort | undefined }) {
  const queryClient = useQueryClient()
  const mutation = useMutation({
    mutationFn: (input: { name: string; unLocode: string; countryCode: string; timeZoneId: string; isActive: boolean }) =>
      target
        ? updateManagedPort(target.id, { ...input, expectedUpdatedAtUtc: target.updatedAtUtc })
        : createManagedPort(input),
    onSuccess: async () => {
      await invalidatePortQueries(queryClient)
      onSaved(target ? 'Porto atualizado com sucesso.' : 'Porto cadastrado com sucesso.')
    },
  })

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    mutation.mutate({
      name: String(data.get('name') ?? '').trim(),
      unLocode: String(data.get('unLocode') ?? '').trim(),
      countryCode: String(data.get('countryCode') ?? '').trim(),
      timeZoneId: String(data.get('timeZoneId') ?? '').trim(),
      isActive: target ? String(data.get('isActive')) === 'true' : true,
    })
  }

  return (
    <EditorShell code="PO" title={target ? 'Editar porto' : 'Cadastrar porto'} description="Use UN/LOCODE, código ISO do país e fuso IANA, por exemplo America/Sao_Paulo." pending={mutation.isPending} error={mutation.error} onCancel={onCancel} onSubmit={submit} submitLabel={target ? 'Salvar alterações' : 'Cadastrar porto'}>
      <label className="field">Nome<input name="name" required maxLength={160} defaultValue={target?.name} /></label>
      <label className="field">UN/LOCODE<input name="unLocode" required minLength={5} maxLength={5} defaultValue={target?.unLocode} /></label>
      <label className="field">Código do país<input name="countryCode" required minLength={2} maxLength={2} defaultValue={target?.countryCode ?? 'BR'} /></label>
      <label className="field">Fuso horário<input name="timeZoneId" required maxLength={80} defaultValue={target?.timeZoneId ?? 'America/Sao_Paulo'} /></label>
      {target && <label className="field">Situação<select name="isActive" defaultValue={String(target.isActive)}><option value="true">Ativo</option><option value="false">Inativo</option></select></label>}
    </EditorShell>
  )
}

function TerminalEditor({ portId, target, onCancel, onSaved }: EditorProps & { portId: string; target?: ManagedTerminal | undefined }) {
  const queryClient = useQueryClient()
  const mutation = useMutation({
    mutationFn: (input: { code: string; name: string; timeZoneId: string; isActive: boolean }) =>
      target
        ? updateManagedTerminal(target.id, { ...input, expectedUpdatedAtUtc: target.updatedAtUtc })
        : createManagedTerminal(portId, input),
    onSuccess: async () => {
      await invalidatePortQueries(queryClient)
      onSaved(target ? 'Terminal atualizado com sucesso.' : 'Terminal cadastrado com sucesso.')
    },
  })

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    mutation.mutate({
      code: String(data.get('code') ?? '').trim(),
      name: String(data.get('name') ?? '').trim(),
      timeZoneId: String(data.get('timeZoneId') ?? '').trim(),
      isActive: target ? String(data.get('isActive')) === 'true' : true,
    })
  }

  return (
    <EditorShell code="TE" title={target ? 'Editar terminal' : 'Cadastrar terminal'} description="O porto de origem é permanente para preservar os vínculos operacionais." pending={mutation.isPending} error={mutation.error} onCancel={onCancel} onSubmit={submit} submitLabel={target ? 'Salvar alterações' : 'Cadastrar terminal'}>
      <label className="field">Código<input name="code" required maxLength={30} defaultValue={target?.code} /></label>
      <label className="field">Nome<input name="name" required maxLength={160} defaultValue={target?.name} /></label>
      <label className="field">Fuso horário<input name="timeZoneId" required maxLength={80} defaultValue={target?.timeZoneId ?? 'America/Sao_Paulo'} /></label>
      {target && <label className="field">Situação<select name="isActive" defaultValue={String(target.isActive)}><option value="true">Ativo</option><option value="false">Inativo</option></select></label>}
    </EditorShell>
  )
}

function BerthEditor({ terminalId, target, onCancel, onSaved }: EditorProps & { terminalId: string; target?: ManagedBerth | undefined }) {
  const queryClient = useQueryClient()
  const mutation = useMutation({
    mutationFn: (input: {
      code: string
      name: string
      usefulLengthMeters: number
      maximumBeamMeters: number
      maximumDraftMeters: number
      supportedVesselTypes: ManagedVesselType[]
      status: ManagedBerthStatus
    }) => target
      ? updateManagedBerth(target.id, { ...input, expectedUpdatedAtUtc: target.updatedAtUtc })
      : createManagedBerth(terminalId, input),
    onSuccess: async () => {
      await invalidatePortQueries(queryClient)
      onSaved(target ? 'Berço atualizado com sucesso.' : 'Berço cadastrado com sucesso.')
    },
  })

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    mutation.mutate({
      code: String(data.get('code') ?? '').trim(),
      name: String(data.get('name') ?? '').trim(),
      usefulLengthMeters: Number(data.get('usefulLengthMeters')),
      maximumBeamMeters: Number(data.get('maximumBeamMeters')),
      maximumDraftMeters: Number(data.get('maximumDraftMeters')),
      supportedVesselTypes: data.getAll('supportedVesselTypes') as ManagedVesselType[],
      status: (target ? String(data.get('status')) : 'Available') as ManagedBerthStatus,
    })
  }

  return (
    <EditorShell code="BE" title={target ? 'Editar berço' : 'Cadastrar berço'} description="Mudanças de capacidade são bloqueadas enquanto houver janelas futuras abertas." pending={mutation.isPending} error={mutation.error} onCancel={onCancel} onSubmit={submit} submitLabel={target ? 'Salvar alterações' : 'Cadastrar berço'}>
      <label className="field">Código<input name="code" required maxLength={30} defaultValue={target?.code} /></label>
      <label className="field">Nome<input name="name" required maxLength={120} defaultValue={target?.name} /></label>
      <label className="field">Comprimento útil (m)<input name="usefulLengthMeters" type="number" required min="0.01" step="0.01" defaultValue={target?.usefulLengthMeters} /></label>
      <label className="field">Boca máxima (m)<input name="maximumBeamMeters" type="number" required min="0.01" step="0.01" defaultValue={target?.maximumBeamMeters} /></label>
      <label className="field">Calado máximo (m)<input name="maximumDraftMeters" type="number" required min="0.01" step="0.01" defaultValue={target?.maximumDraftMeters} /></label>
      {target && <label className="field">Situação<select name="status" defaultValue={target.status}>{Object.entries(berthStatusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>}
      <fieldset className="field span-2 vessel-type-options">
        <legend>Tipos de navio permitidos</legend>
        <small>Sem seleção, o berço aceita qualquer tipo compatível com suas dimensões.</small>
        <div>
          {vesselTypes.map((item) => (
            <label key={item.value}>
              <input name="supportedVesselTypes" type="checkbox" value={item.value} defaultChecked={target?.supportedVesselTypes.includes(item.value)} />
              {item.label}
            </label>
          ))}
        </div>
      </fieldset>
    </EditorShell>
  )
}

function EditorShell({
  code,
  title,
  description,
  pending,
  error,
  onCancel,
  onSubmit,
  submitLabel,
  children,
}: Pick<EditorProps, 'onCancel'> & {
  code: string
  title: string
  description: string
  pending: boolean
  error: unknown
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  submitLabel: string
  children: ReactNode
}) {
  return (
    <form className="content-card entity-form master-data-editor" onSubmit={onSubmit}>
      <div className="form-section-heading">
        <span>{code}</span>
        <div><strong>{title}</strong><small>{description}</small></div>
      </div>
      <div className="form-grid">{children}</div>
      <FormError error={error} />
      <footer className="form-actions">
        <button className="secondary-action" type="button" onClick={onCancel}>Cancelar</button>
        <button className="primary-action" type="submit" disabled={pending}>{pending ? 'Salvando…' : submitLabel}</button>
      </footer>
    </form>
  )
}

function State({ active }: { active: boolean }) {
  return <span className={`record-state ${active ? 'active' : ''}`}>{active ? 'Ativo' : 'Inativo'}</span>
}

async function invalidatePortQueries(queryClient: ReturnType<typeof useQueryClient>) {
  await queryClient.invalidateQueries({ queryKey: ['master-ports'] })
  await queryClient.invalidateQueries({ queryKey: ['ports'] })
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  }).format(new Date(value))
}
