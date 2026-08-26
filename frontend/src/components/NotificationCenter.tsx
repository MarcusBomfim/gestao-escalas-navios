import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getAccessToken } from '../api/client'
import {
  getNotificationCenter,
  markAllNotificationsRead,
  markNotificationRead,
} from '../api/portManagement'
import type {
  NotificationCenter as NotificationCenterData,
  NotificationItem,
  OperationalAlertSeverity,
} from '../api/types'
import { environment } from '../config/environment'

type RealtimeStatus = 'connecting' | 'connected' | 'reconnecting' | 'offline'

const severityLabels: Record<OperationalAlertSeverity, string> = {
  Critical: 'Crítico',
  Warning: 'Atenção',
  Info: 'Informativo',
}

const connectionLabels: Record<RealtimeStatus, string> = {
  connecting: 'Conectando',
  connected: 'Tempo real ativo',
  reconnecting: 'Reconectando',
  offline: 'Atualização periódica',
}

export function NotificationCenter() {
  const [isOpen, setIsOpen] = useState(false)
  const [realtimeStatus, setRealtimeStatus] = useState<RealtimeStatus>('connecting')
  const queryClient = useQueryClient()
  const notifications = useQuery({
    queryKey: ['notifications'],
    queryFn: getNotificationCenter,
    refetchInterval: 60_000,
  })

  useEffect(() => {
    let isActive = true
    let retryTimer: ReturnType<typeof setTimeout> | undefined
    const connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace(/\/$/, '')}/hubs/control-tower`, {
        accessTokenFactory: () => getAccessToken() ?? '',
      })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('ControlTowerInvalidated', () => {
      void queryClient.invalidateQueries({ queryKey: ['control-tower'] })
      void queryClient.invalidateQueries({ queryKey: ['notifications'] })
    })
    connection.onreconnecting(() => isActive && setRealtimeStatus('reconnecting'))
    connection.onreconnected(() => {
      if (isActive) setRealtimeStatus('connected')
      void queryClient.invalidateQueries({ queryKey: ['control-tower'] })
      void queryClient.invalidateQueries({ queryKey: ['notifications'] })
    })
    connection.onclose(() => isActive && setRealtimeStatus('offline'))

    const startConnection = async () => {
      if (!isActive || connection.state !== HubConnectionState.Disconnected) return
      try {
        await connection.start()
        if (isActive) setRealtimeStatus('connected')
      } catch {
        if (!isActive) return
        setRealtimeStatus('offline')
        retryTimer = setTimeout(() => void startConnection(), 10_000)
      }
    }
    void startConnection()

    return () => {
      isActive = false
      if (retryTimer) clearTimeout(retryTimer)
      if (connection.state !== HubConnectionState.Disconnected) {
        void connection.stop()
      }
    }
  }, [queryClient])

  useEffect(() => {
    if (!isOpen) return undefined
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setIsOpen(false)
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isOpen])

  const updateCache = (data: NotificationCenterData) => {
    queryClient.setQueryData(['notifications'], data)
  }
  const markRead = useMutation({
    mutationFn: markNotificationRead,
    onSuccess: updateCache,
  })
  const markAllRead = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: updateCache,
  })
  const unreadCount = notifications.data?.unreadCount ?? 0

  return (
    <>
      <button
        className="notification-trigger"
        type="button"
        aria-label={`Notificações${unreadCount > 0 ? `, ${unreadCount} não lidas` : ''}`}
        aria-expanded={isOpen}
        onClick={() => setIsOpen(true)}
      >
        <BellIcon />
        {unreadCount > 0 && <span>{unreadCount > 9 ? '9+' : unreadCount}</span>}
      </button>

      {isOpen && (
        <div className="notification-layer">
          <button className="notification-backdrop" type="button" aria-label="Fechar notificações" onClick={() => setIsOpen(false)} />
          <aside className="notification-drawer" role="dialog" aria-modal="true" aria-labelledby="notification-title">
            <header>
              <div><span>Centro operacional</span><h2 id="notification-title">Notificações</h2></div>
              <button type="button" aria-label="Fechar" onClick={() => setIsOpen(false)}>×</button>
            </header>

            <div className="notification-toolbar">
              <span className={`realtime-state ${realtimeStatus}`}><i />{connectionLabels[realtimeStatus]}</span>
              {unreadCount > 0 && <button type="button" disabled={markAllRead.isPending} onClick={() => markAllRead.mutate()}>Marcar todas como lidas</button>}
            </div>

            <div className="notification-scroll">
              {notifications.isPending && <div className="notification-empty">Carregando notificações…</div>}
              {notifications.isError && <div className="notification-empty error">Não foi possível carregar as notificações.</div>}
              {notifications.data?.items.length === 0 && <div className="notification-empty"><strong>Nenhum alerta ativo</strong><span>A operação está sem notificações no momento.</span></div>}
              {notifications.data?.items.map((item) => (
                <NotificationCard
                  key={item.id}
                  item={item}
                  isPending={markRead.isPending}
                  onRead={() => markRead.mutate(item.id)}
                  onNavigate={() => setIsOpen(false)}
                />
              ))}
            </div>
          </aside>
        </div>
      )}
    </>
  )
}

function NotificationCard({ item, isPending, onRead, onNavigate }: {
  item: NotificationItem
  isPending: boolean
  onRead: () => void
  onNavigate: () => void
}) {
  return (
    <article className={`notification-card ${item.severity.toLowerCase()}${item.isRead ? ' read' : ''}`}>
      <i className="notification-severity" aria-hidden="true" />
      <div>
        <header><span>{severityLabels[item.severity]}</span><time>{formatTime(item.detectedAtUtc)}</time></header>
        <h3>{item.title}</h3>
        <p>{item.description}</p>
        <small>{item.vesselName} · {item.portCallPublicCode}</small>
        <footer>
          {!item.isRead && <button type="button" disabled={isPending} onClick={onRead}>Marcar como lida</button>}
          <Link to={item.actionPath} onClick={onNavigate}>Abrir escala →</Link>
        </footer>
      </div>
    </article>
  )
}

function BellIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4" />
    </svg>
  )
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' }).format(new Date(value))
}
