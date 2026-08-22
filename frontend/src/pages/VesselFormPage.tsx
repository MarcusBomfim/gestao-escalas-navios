import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { getVessel, registerVessel, updateVessel } from '../api/portManagement'
import type { Vessel, VesselInput } from '../api/types'
import { FormError } from '../components/FormFeedback'
import { QueryError, TableSkeleton } from '../components/QueryFeedback'

const vesselTypes = [
  ['ContainerShip', 'Porta-contêineres'],
  ['BulkCarrier', 'Graneleiro'],
  ['Tanker', 'Petroleiro'],
  ['GeneralCargo', 'Carga geral'],
  ['RoRo', 'Ro-Ro'],
  ['Passenger', 'Passageiros'],
  ['Offshore', 'Apoio marítimo'],
  ['Other', 'Outro'],
] as const

export function VesselFormPage() {
  const { id } = useParams()
  const vessel = useQuery({
    queryKey: ['vessel', id],
    queryFn: () => getVessel(id!),
    enabled: Boolean(id),
  })

  if (id && vessel.isPending) {
    return <section className="content-card"><TableSkeleton rows={5} /></section>
  }

  if (id && vessel.isError) {
    return <section className="content-card"><QueryError error={vessel.error} /></section>
  }

  return <VesselEditor vessel={vessel.data} />
}

function VesselEditor({ vessel }: { vessel: Vessel | undefined }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const mutation = useMutation({
    mutationFn: (input: VesselInput) => vessel
      ? updateVessel(vessel.id, input)
      : registerVessel(input),
    onSuccess: async (savedVessel) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['vessels'] }),
        queryClient.invalidateQueries({ queryKey: ['vessel', savedVessel.id] }),
      ])
      navigate('/navios', { replace: true })
    },
  })

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    mutation.mutate({
      name: requiredText(data, 'name'),
      imoNumber: optionalText(data, 'imoNumber'),
      flagCode: requiredText(data, 'flagCode').toUpperCase(),
      type: requiredText(data, 'type'),
      lengthOverallMeters: requiredNumber(data, 'lengthOverallMeters'),
      beamMeters: requiredNumber(data, 'beamMeters'),
      maximumDraftMeters: requiredNumber(data, 'maximumDraftMeters'),
      callSign: optionalText(data, 'callSign')?.toUpperCase() ?? null,
      mmsi: optionalText(data, 'mmsi'),
    })
  }

  return (
    <div className="page-stack narrow-page">
      <section className="page-introduction form-introduction">
        <div>
          <p>Cadastro operacional</p>
          <h2>{vessel ? 'Editar navio' : 'Cadastrar novo navio'}</h2>
          <span>Informe as dimensões e identificadores usados no planejamento portuário.</span>
        </div>
        <Link className="secondary-action" to="/navios">Voltar para navios</Link>
      </section>

      <form className="content-card entity-form" onSubmit={handleSubmit}>
        <div className="form-section-heading">
          <span>01</span>
          <div><strong>Identificação</strong><small>Dados públicos e classificação do navio.</small></div>
        </div>
        <div className="form-grid">
          <label className="field span-2">Nome do navio<input name="name" required maxLength={160} defaultValue={vessel?.name} /></label>
          <label className="field">Número IMO<input name="imoNumber" placeholder="IMO9074729" pattern="(?:IMO)?[0-9]{7}" defaultValue={vessel?.imoNumber ?? ''} /></label>
          <label className="field">Bandeira (ISO 2)<input name="flagCode" required maxLength={2} minLength={2} pattern="[A-Za-z]{2}" defaultValue={vessel?.flagCode} /></label>
          <label className="field span-2">Tipo<select name="type" required defaultValue={vessel?.type ?? 'ContainerShip'}>{vesselTypes.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
          <label className="field">Indicativo de chamada<input name="callSign" maxLength={20} defaultValue={vessel?.callSign ?? ''} /></label>
          <label className="field">MMSI<input name="mmsi" inputMode="numeric" pattern="[0-9]{9}" maxLength={9} defaultValue={vessel?.mmsi ?? ''} /></label>
        </div>

        <div className="form-section-heading">
          <span>02</span>
          <div><strong>Dimensões operacionais</strong><small>Valores em metros, utilizados para verificar compatibilidade.</small></div>
        </div>
        <div className="form-grid three-columns">
          <label className="field">Comprimento total<input name="lengthOverallMeters" type="number" min="0.1" step="0.1" required defaultValue={vessel?.lengthOverallMeters} /></label>
          <label className="field">Boca<input name="beamMeters" type="number" min="0.1" step="0.1" required defaultValue={vessel?.beamMeters} /></label>
          <label className="field">Calado máximo<input name="maximumDraftMeters" type="number" min="0.1" step="0.1" required defaultValue={vessel?.maximumDraftMeters} /></label>
        </div>

        <FormError error={mutation.error} />
        <footer className="form-actions">
          <Link className="secondary-action" to="/navios">Cancelar</Link>
          <button className="primary-action" type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Salvando…' : vessel ? 'Salvar alterações' : 'Cadastrar navio'}
          </button>
        </footer>
      </form>
    </div>
  )
}

function requiredText(data: FormData, field: string) {
  return String(data.get(field) ?? '').trim()
}

function optionalText(data: FormData, field: string) {
  const value = requiredText(data, field)
  return value || null
}

function requiredNumber(data: FormData, field: string) {
  return Number(data.get(field))
}
