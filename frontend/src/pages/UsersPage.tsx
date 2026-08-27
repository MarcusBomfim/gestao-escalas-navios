import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  createUser,
  getUserManagementOptions,
  listUsers,
  updateUser,
} from '../api/portManagement'
import type {
  ManagedUser,
  SecurityRole,
  UserManagementOptions,
} from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { FormError } from '../components/FormFeedback'
import { Pagination } from '../components/Pagination'
import { EmptyState, QueryError, TableSkeleton } from '../components/QueryFeedback'

const roleLabels: Record<SecurityRole, string> = {
  Administrator: 'Administrador',
  Planner: 'Planejador',
  Operator: 'Operador',
  Viewer: 'Visitante',
}

const organizationTypeLabels: Record<string, string> = {
  PortAuthority: 'Autoridade portuária',
  TerminalOperator: 'Terminal',
  PortOperator: 'Operador portuário',
  ShippingLine: 'Armador',
  ShippingAgency: 'Agência marítima',
}

type ActiveFilter = 'all' | 'active' | 'inactive'

export function UsersPage() {
  const { user: currentUser } = useAuth()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [role, setRole] = useState<SecurityRole | ''>('')
  const [activeFilter, setActiveFilter] = useState<ActiveFilter>('all')
  const [editing, setEditing] = useState<ManagedUser | 'new' | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const users = useQuery({
    queryKey: ['users', { page, search, role, activeFilter }],
    queryFn: () => listUsers({
      page,
      pageSize: 10,
      search,
      ...(role ? { role } : {}),
      ...(activeFilter === 'all' ? {} : { isActive: activeFilter === 'active' }),
    }),
  })
  const options = useQuery({
    queryKey: ['user-management-options'],
    queryFn: getUserManagementOptions,
  })

  const handleSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSearch(searchInput.trim())
    setPage(1)
  }

  const clearFilters = () => {
    setSearch('')
    setSearchInput('')
    setRole('')
    setActiveFilter('all')
    setPage(1)
  }

  const hasFilters = Boolean(search || role || activeFilter !== 'all')

  return (
    <div className="page-stack">
      <section className="page-introduction">
        <div>
          <p>Administração de acesso</p>
          <h2>{users.data?.totalItems ?? '—'} usuários cadastrados</h2>
          <span>Controle os perfis, organizações e acessos ativos da plataforma.</span>
        </div>
        <button
          className="primary-action"
          type="button"
          onClick={() => { setEditing('new'); setSuccessMessage(null) }}
        >
          Cadastrar usuário
        </button>
      </section>

      {successMessage && (
        <div className="form-feedback success" role="status">
          {successMessage}
        </div>
      )}

      {editing && options.data && (
        <UserEditor
          key={editing === 'new' ? 'new' : editing.id}
          target={editing === 'new' ? undefined : editing}
          currentUserId={currentUser?.id ?? ''}
          options={options.data}
          onCancel={() => setEditing(null)}
          onSaved={(message) => {
            setEditing(null)
            setSuccessMessage(message)
          }}
        />
      )}

      {editing && options.isPending && <section className="content-card"><TableSkeleton rows={4} /></section>}
      {options.isError && editing && <section className="content-card"><QueryError error={options.error} /></section>}

      <section className="content-card data-card">
        <header className="data-toolbar user-filters">
          <form className="search-form" onSubmit={handleSearch}>
            <label className="sr-only" htmlFor="user-search">Buscar usuário</label>
            <input
              id="user-search"
              type="search"
              placeholder="Buscar por nome ou e-mail…"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
            />
            <button type="submit">Buscar</button>
          </form>

          <label className="select-filter">
            <span className="sr-only">Filtrar por perfil</span>
            <select
              value={role}
              onChange={(event) => { setRole(event.target.value as SecurityRole | ''); setPage(1) }}
            >
              <option value="">Todos os perfis</option>
              {Object.entries(roleLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </label>

          <label className="select-filter">
            <span className="sr-only">Filtrar por situação</span>
            <select
              value={activeFilter}
              onChange={(event) => { setActiveFilter(event.target.value as ActiveFilter); setPage(1) }}
            >
              <option value="all">Todas as situações</option>
              <option value="active">Ativos</option>
              <option value="inactive">Bloqueados</option>
            </select>
          </label>

          {hasFilters && <button className="clear-filter" type="button" onClick={clearFilters}>Limpar filtros</button>}
        </header>

        {users.isPending && <TableSkeleton rows={7} />}
        {users.isError && <QueryError error={users.error} />}
        {users.data?.items.length === 0 && (
          <EmptyState title="Nenhum usuário encontrado" description="Altere os filtros utilizados na consulta." />
        )}

        {users.data && users.data.items.length > 0 && (
          <div className="table-scroll">
            <table>
              <thead>
                <tr><th>Usuário</th><th>Perfil</th><th>Organização</th><th>Criado em</th><th>Situação</th><th><span className="sr-only">Ações</span></th></tr>
              </thead>
              <tbody>
                {users.data.items.map((managedUser) => {
                  const primaryRole = managedUser.roles[0]
                  const isSelf = managedUser.id === currentUser?.id
                  return (
                    <tr key={managedUser.id}>
                      <td>
                        <strong>{managedUser.displayName}{isSelf ? ' (você)' : ''}</strong>
                        <small>{managedUser.email}</small>
                      </td>
                      <td>{primaryRole ? roleLabels[primaryRole] : 'Sem perfil'}</td>
                      <td>{managedUser.organizationName ?? 'Escopo global'}</td>
                      <td>{formatDate(managedUser.createdAtUtc)}</td>
                      <td>
                        <span className={`record-state ${managedUser.isActive ? 'active' : ''}`}>
                          {managedUser.isActive ? 'Ativo' : 'Bloqueado'}
                        </span>
                      </td>
                      <td className="table-action">
                        <button
                          type="button"
                          onClick={() => { setEditing(managedUser); setSuccessMessage(null) }}
                        >
                          Editar
                        </button>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}

        <Pagination page={page} totalPages={users.data?.totalPages ?? 0} onChange={setPage} />
      </section>
    </div>
  )
}

interface UserEditorProps {
  target: ManagedUser | undefined
  currentUserId: string
  options: UserManagementOptions
  onCancel: () => void
  onSaved: (message: string) => void
}

function UserEditor({ target, currentUserId, options, onCancel, onSaved }: UserEditorProps) {
  const queryClient = useQueryClient()
  const initialRole = target?.roles[0] ?? 'Viewer'
  const [selectedRole, setSelectedRole] = useState<SecurityRole>(initialRole)
  const isSelf = target?.id === currentUserId
  const preservesGlobalViewer = Boolean(
    target &&
    target.organizationId === null &&
    target.roles.includes('Viewer') &&
    selectedRole === 'Viewer',
  )
  const organizationRequired = selectedRole !== 'Administrator' && !preservesGlobalViewer
  const mutation = useMutation({
    mutationFn: (input: {
      displayName: string
      email: string
      password: string
      organizationId: string | null
      isActive: boolean
    }) => target
      ? updateUser(target.id, {
        displayName: input.displayName,
        role: selectedRole,
        organizationId: selectedRole === 'Administrator' ? null : input.organizationId,
        isActive: input.isActive,
        expectedVersion: target.version,
      })
      : createUser({
        displayName: input.displayName,
        email: input.email,
        password: input.password,
        role: selectedRole,
        organizationId: selectedRole === 'Administrator' ? null : input.organizationId,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['users'] })
      onSaved(target ? 'Usuário atualizado com sucesso.' : 'Usuário cadastrado com sucesso.')
    },
  })

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    mutation.mutate({
      displayName: String(data.get('displayName') ?? '').trim(),
      email: String(data.get('email') ?? '').trim(),
      password: String(data.get('password') ?? ''),
      organizationId: String(data.get('organizationId') ?? '') || null,
      isActive: target ? (isSelf ? target.isActive : String(data.get('isActive')) === 'true') : true,
    })
  }

  return (
    <form className="content-card entity-form user-editor" onSubmit={(event) => void handleSubmit(event)}>
      <div className="form-section-heading">
        <span>AC</span>
        <div>
          <strong>{target ? 'Editar usuário' : 'Cadastrar usuário'}</strong>
          <small>{target ? 'Alterações de acesso encerram as sessões anteriores.' : 'A senha inicial poderá ser redefinida pelo próprio usuário.'}</small>
        </div>
      </div>

      <div className="form-grid">
        <label className="field">
          Nome
          <input name="displayName" required maxLength={160} defaultValue={target?.displayName} />
        </label>
        <label className="field">
          E-mail
          <input
            name="email"
            type="email"
            autoComplete="off"
            required={!target}
            disabled={Boolean(target)}
            maxLength={320}
            defaultValue={target?.email}
          />
        </label>

        {!target && (
          <label className="field">
            Senha inicial
            <input name="password" type="password" autoComplete="new-password" minLength={12} maxLength={256} required />
          </label>
        )}

        <label className="field">
          Perfil
          <select
            name="role"
            value={selectedRole}
            disabled={isSelf}
            onChange={(event) => setSelectedRole(event.target.value as SecurityRole)}
          >
            {options.roles.map((role) => <option key={role} value={role}>{roleLabels[role]}</option>)}
          </select>
          {isSelf && <small>Seu próprio perfil administrativo não pode ser removido.</small>}
        </label>

        <label className="field">
          Organização
          <select
            name="organizationId"
            disabled={selectedRole === 'Administrator'}
            required={organizationRequired}
            defaultValue={target?.organizationId ?? ''}
          >
            <option value="">
              {selectedRole === 'Administrator' || preservesGlobalViewer
                ? 'Escopo global'
                : 'Selecione uma organização'}
            </option>
            {options.organizations.map((organization) => (
              <option key={organization.id} value={organization.id}>
                {organization.name} · {organizationTypeLabels[organization.type] ?? organization.type}
              </option>
            ))}
          </select>
        </label>

        {target && (
          <label className="field">
            Situação
            <select name="isActive" defaultValue={String(target.isActive)} disabled={isSelf}>
              <option value="true">Ativo</option>
              <option value="false">Bloqueado</option>
            </select>
            {isSelf && <small>Você não pode bloquear a própria conta.</small>}
          </label>
        )}
      </div>

      {!target && (
        <p className="form-information">
          <strong>Política de senha</strong>
          <span>Mínimo de 12 caracteres, com maiúscula, minúscula, número e símbolo.</span>
        </p>
      )}

      <FormError error={mutation.error} />
      <footer className="form-actions">
        <button className="secondary-action" type="button" onClick={onCancel}>Cancelar</button>
        <button className="primary-action" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? 'Salvando…' : target ? 'Salvar alterações' : 'Cadastrar usuário'}
        </button>
      </footer>
    </form>
  )
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  }).format(new Date(value))
}
