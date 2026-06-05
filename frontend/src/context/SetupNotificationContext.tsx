import React, { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { apiBaseUrl, getToken, setups } from '../api'
import { useAuth } from './AuthContext'
import { useToast } from './ToastContext'
import { useTabBlink } from '../hooks/useTabBlink'
import { isFullSetup, normalizeOpportunity, type SetupOpportunity } from '../types/setup'

type SetupNotificationContextValue = {
  opportunities: SetupOpportunity[]
  lastFullSetup: SetupOpportunity | null
  latestOpportunityId: string | null
  latestOpportunity: SetupOpportunity | null
  opportunityVersion: number
  alertActive: boolean
  refreshOpportunities: () => Promise<void>
}

const SetupNotificationContext = createContext<SetupNotificationContextValue | null>(null)

function handleFullSetupAlert(
  opp: SetupOpportunity,
  showToast: ReturnType<typeof useToast>['showToast'],
  startBlink: (title: string) => void,
  setAlertActive: (v: boolean) => void
) {
  const title = `Setup 6/6 · ${opp.symbol} ${opp.direction}`
  const message = opp.message || `${opp.side} @ ${opp.entryPrice} — alle Bedingungen erfüllt.`

  showToast({
    title,
    message,
    variant: 'success',
    href: '/setups'
  })

  startBlink(`🔔 ${opp.symbol} Setup 6/6!`)
  setAlertActive(true)
  window.setTimeout(() => setAlertActive(false), 6000)
}

/** Global SignalR listener for institutional setup opportunities. */
export function SetupNotificationProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth()
  const { showToast } = useToast()
  const { startBlink } = useTabBlink()
  const [opportunities, setOpportunities] = useState<SetupOpportunity[]>([])
  const [lastFullSetup, setLastFullSetup] = useState<SetupOpportunity | null>(null)
  const [alertActive, setAlertActive] = useState(false)
  const [latestOpportunityId, setLatestOpportunityId] = useState<string | null>(null)
  const [latestOpportunity, setLatestOpportunity] = useState<SetupOpportunity | null>(null)
  const [opportunityVersion, setOpportunityVersion] = useState(0)
  const connectionRef = useRef<HubConnection | null>(null)
  const notifiedIdsRef = useRef<Set<string>>(new Set())

  const refreshOpportunities = useCallback(async () => {
    try {
      const opps = await setups.opportunities()
      setOpportunities((opps ?? []).map((o: Record<string, unknown>) => normalizeOpportunity(o)))
    } catch {
      /* ignore when offline */
    }
  }, [])

  const onOpportunity = useCallback((raw: Record<string, unknown>) => {
    const opp = normalizeOpportunity(raw)
    setOpportunities(prev => [opp, ...prev.filter(o => o.id !== opp.id)])
    setLatestOpportunityId(opp.id)
    setLatestOpportunity(opp)
    setOpportunityVersion(v => v + 1)

    void refreshOpportunities()

    if (!isFullSetup(opp)) return
    if (notifiedIdsRef.current.has(opp.id)) return

    notifiedIdsRef.current.add(opp.id)
    setLastFullSetup(opp)
    handleFullSetupAlert(opp, showToast, startBlink, setAlertActive)
  }, [showToast, startBlink, refreshOpportunities])

  useEffect(() => {
    if (!isAuthenticated) {
      setOpportunities([])
      setLastFullSetup(null)
      notifiedIdsRef.current.clear()
      connectionRef.current?.stop().catch(() => undefined)
      connectionRef.current = null
      return
    }

    refreshOpportunities()

    const connection = new HubConnectionBuilder()
      .withUrl(`${apiBaseUrl}/hubs/paper-trading`, { accessTokenFactory: () => getToken() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('SetupDetected', onOpportunity)

    connection
      .start()
      .then(() => connection.invoke('SubscribeToSetups'))
      .catch(err => console.error('SignalR setup notifications failed', err))

    connectionRef.current = connection
    return () => {
      connection.off('SetupDetected', onOpportunity)
      connection.stop().catch(() => undefined)
    }
  }, [isAuthenticated, onOpportunity, refreshOpportunities])

  const value = useMemo(
    () => ({
      opportunities,
      lastFullSetup,
      latestOpportunityId,
      latestOpportunity,
      opportunityVersion,
      alertActive,
      refreshOpportunities
    }),
    [opportunities, lastFullSetup, latestOpportunityId, latestOpportunity, opportunityVersion, alertActive, refreshOpportunities]
  )

  return (
    <SetupNotificationContext.Provider value={value}>
      {children}
    </SetupNotificationContext.Provider>
  )
}

/** Returns live setup opportunities and alert state. */
export function useSetupNotifications(): SetupNotificationContextValue {
  const ctx = useContext(SetupNotificationContext)
  if (!ctx) throw new Error('useSetupNotifications must be used within SetupNotificationProvider')
  return ctx
}
