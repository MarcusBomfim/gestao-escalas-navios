import { useRef } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
import { createPortCall, listPorts, listVessels } from '../api/portManagement'
import type { PortCallInput } from '../api/types'
import { FormError } from '../components/FormFeedback'
import { portCallPurposes } from '../components/portCallOptions'
import { QueryError, TableSkeleton } from '../components/QueryFeedback'

export function PortCallFormPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const idempotencyKey = useRef(crypto.randomUUID())
  const vessels = useQuery({
    queryKey: ['vessels', { page: 1, pageSize: 100, selection: true }],
    queryFn: () => listVessels({ page: 1, pageSize: 100 }),
  })
  const ports = useQuery({ queryKey: ['ports'], queryFn: listPorts })
  const mutation = useMutation({
    mutationFn: (input: PortCallInput) => createPortCall(input, idempotencyKey.current),
    onSuccess: async (portCall) => {
      await queryClient.invalidateQueries({ queryKey: ['port-calls'] })
      navigate(`/escalas/${portCall.publicCode}`, { replace: true })
    },
  })

  if (vessels.isPending || ports.isPending) {
    return <section className="content-card"><TableSkeleton rows={5} /></section>
  }

  if (vessels.isError || ports.isError) {
    return <section className="content-card"><QueryError error={vessels.error ?? ports.error} /></section>
  }

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    mutation.mutate({
      vesselId: String(data.get('vesselId')),
      portId: String(data.get('portId')),
      purpose: String(data.get('purpose')),
      voyageNumber: optionalText(data, 'voyageNumber'),
      previousPortUnLocode: optionalText(data, 'previousPortUnLocode'),
      nextPortUnLocode: optionalText(data, 'nextPortUnLocode'),
    })
  }

  return (
    <div className="page-stack narrow-page">
      <section className="page-introduction form-introduction">
        <div>
          <p>Programação portuária</p>
          <h2>Registrar nova escala</h2>
          <span>A escala nasce como rascunho e segue um fluxo operacional auditável.</span>
        </div>
        <Link className="secondary-action" to="/escalas">Voltar para escalas</Link>
      </section>

      <form className="content-card entity-form" onSubmit={handleSubmit}>
        <div className="form-section-heading">
          <span>01</span>
          <div><strong>Operação</strong><small>Selecione o navio, o porto e a finalidade da escala.</small></div>
        </div>
        <div className="form-grid">
          <label className="field span-2">Navio<select name="vesselId" required defaultValue=""><option value="" disabled>Selecione um navio</option>{vessels.data.items.map((vessel) => <option key={vessel.id} value={vessel.id}>{vessel.name} · {vessel.imoNumber ?? vessel.flagCode}</option>)}</select></label>
          <label className="field span-2">Porto<select name="portId" required defaultValue=""><option value="" disabled>Selecione um porto</option>{ports.data.map((port) => <option key={port.id} value={port.id}>{port.name} · {port.unLocode}</option>)}</select></label>
          <label className="field span-2">Finalidade<select name="purpose" required defaultValue="CargoOperation">{portCallPurposes.map((purpose) => <option key={purpose.value} value={purpose.value}>{purpose.label}</option>)}</select></label>
        </div>

        <div className="form-section-heading">
          <span>02</span>
          <div><strong>Viagem e rota</strong><small>Informações de apoio à previsão da chegada e da saída.</small></div>
        </div>
        <div className="form-grid three-columns">
          <label className="field">Número da viagem<input name="voyageNumber" maxLength={50} placeholder="ASL-2026-047" /></label>
          <label className="field">Porto anterior<input name="previousPortUnLocode" maxLength={5} minLength={5} pattern="[A-Za-z0-9]{5}" placeholder="BRRIO" /></label>
          <label className="field">Próximo porto<input name="nextPortUnLocode" maxLength={5} minLength={5} pattern="[A-Za-z0-9]{5}" placeholder="SGSIN" /></label>
        </div>

        <div className="form-information">
          <strong>Criação protegida contra duplicidade</strong>
          <span>Uma chave idempotente identifica esta operação mesmo se houver repetição de rede.</span>
        </div>
        <FormError error={mutation.error} />
        <footer className="form-actions">
          <Link className="secondary-action" to="/escalas">Cancelar</Link>
          <button className="primary-action" type="submit" disabled={mutation.isPending || vessels.data.items.length === 0}>
            {mutation.isPending ? 'Registrando…' : 'Registrar escala'}
          </button>
        </footer>
      </form>
    </div>
  )
}

function optionalText(data: FormData, field: string) {
  const value = String(data.get(field) ?? '').trim().toUpperCase()
  return value || null
}
