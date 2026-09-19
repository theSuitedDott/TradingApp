import React, { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { accounts, apiBaseUrl, getToken, setups } from '../api'
import { useAuth } from './AuthContext'
import { useToast } from './ToastContext'
import { useTabBlink } from '../hooks/useTabBlink'
import { isFullSetup, normalizeOpportunity, type SetupOpportunity } from '../types/setup'
import { hasPositionForSymbol, normalizeOpenPosition, type OpenPosition } from '../types/position'
import { requestNotificationPermission, showBrowserNotification } from '../utils/browserNotify'
import type { ChartExitAlert } from '../utils/chartSignalMarkers'

type SetupNotificationContextValue = {
  opportunities: SetupOpportunity[]
  lastFullSetup: SetupOpportunity | null
  latestOpportunityId: string | null
  latestOpportunity: SetupOpportunity | null
  opportunityVersion: number
  alertActive: boolean
  openPositions: OpenPosition[]
  exitAlerts: ChartExitAlert[]
  sellAlertsEnabled: boolean
  hasOpenPosition: (symbol: string) => boolean
  refreshOpportunities: () => Promise<void>
  refreshPositions: () => Promise<void>
}

const SetupNotificationContext = createContext<SetupNotificationContextValue | null>(null)

/** Global trade alerts: buy on 6/6 setup (no open position), sell via minute scanner + manual check. */
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
  const [openPositions, setOpenPositions] = useState<OpenPosition[]>([])
  const [exitAlerts, setExitAlerts] = useState<ChartExitAlert[]>([])
  const [accountId, setAccountId] = useState<string | null>(null)

  const connectionRef = useRef<HubConnection | null>(null)
  const notifiedBuyIdsRef = useRef<Set<string>>(new Set())
  const openPositionsRef = useRef<OpenPosition[]>([])

  const sellAlertsEnabled = openPositions.length > 0

  const hasOpenPosition = useCallback(
    (symbol: string) => hasPositionForSymbol(openPositions, symbol),
    [openPositions]
  )

  const refreshPositions = useCallback(async () => {
    try {
      const accs = await accounts.list()
      const list = accs ?? []
      const defaultAcc = list.find((a: { isDefault?: boolean }) => a.isDefault) ?? list[0]
      if (!defaultAcc?.id) {
        setAccountId(null)
        setOpenPositions([])
        return
      }
      setAccountId(defaultAcc.id)
      const posRaw = await accounts.positions(defaultAcc.id, true)
      const positions = (posRaw ?? []).map((p: Record<string, unknown>) => normalizeOpenPosition(p))
      setOpenPositions(positions)
      openPositionsRef.current = positions

    } catch {
      /* offline */
    }
  }, [])

  const refreshOpportunities = useCallback(async () => {
    try {
      const opps = await setups.opportunities()
      setOpportunities((opps ?? []).map((o: Record<string, unknown>) => normalizeOpportunity(o)))
    } catch {
      /* offline */
    }
  }, [])

  const triggerBuyAlert = useCallback((opp: SetupOpportunity) => {
    const title = `KAUF-SIGNAL · ${opp.symbol} ${opp.direction}`
    const message =
      opp.message ||
      `Alle 6 Bedingungen erfüllt — ${opp.side} @ ${opp.entryPrice}. Stop ${opp.stopLossPrice}, Ziel ${opp.takeProfitPrice}.`

    showToast({ title, message, variant: 'success', href: '/setups' })
    startBlink(`🟢 KAUF ${opp.symbol}`)
    setAlertActive(true)
    window.setTimeout(() => setAlertActive(false), 6000)

    showBrowserNotification({ title, body: message, type: 'buy' })
  }, [showToast, startBlink])

  const onExitSignal = useCallback((raw: Record<string, unknown>) => {
    const reason = String(raw.reason ?? raw.Reason ?? '')
    const symbol = String(raw.symbol ?? raw.Symbol ?? '')
    const message = String(raw.message ?? raw.Message ?? 'Verkaufssignal')
    const isStop = reason.toLowerCase().includes('stop')

    const title = `VERKAUF-SIGNAL · ${isStop ? 'Stop-Loss' : 'Take-Profit'} ${symbol}`
    showToast({
      title,
      message,
      variant: isStop ? 'warning' : 'success',
      href: '/setups'
    })
    startBlink(`🔴 VERKAUF ${symbol}`)
    setAlertActive(true)
    window.setTimeout(() => setAlertActive(false), 6000)

    showBrowserNotification({
      title,
      body: message,
      type: isStop ? 'sell-sl' : 'sell-tp'
    })

    setExitAlerts(prev => [
      {
        id: `${symbol}-${reason}-${Date.now()}`,
        symbol,
        reason,
        message,
        detectedAt: new Date().toISOString()
      },
      ...prev
    ].slice(0, 50))
  }, [showToast, startBlink])

  const onOpportunity = useCallback((raw: Record<string, unknown>) => {
    const opp = normalizeOpportunity(raw)
    setOpportunities(prev => [opp, ...prev.filter(o => o.id !== opp.id)])
    setLatestOpportunityId(opp.id)
    setLatestOpportunity(opp)
    setOpportunityVersion(v => v + 1)
    void refreshOpportunities()

    if (!isFullSetup(opp)) return
    if (notifiedBuyIdsRef.current.has(opp.id)) return

    if (hasPositionForSymbol(openPositionsRef.current, opp.symbol)) {
      return
    }

    notifiedBuyIdsRef.current.add(opp.id)
    setLastFullSetup(opp)
    triggerBuyAlert(opp)
  }, [refreshOpportunities, triggerBuyAlert])

  useEffect(() => {
    openPositionsRef.current = openPositions
  }, [openPositions])

  useEffect(() => {
    if (!isAuthenticated) {
      setOpportunities([])
      setLastFullSetup(null)
      setOpenPositions([])
      setExitAlerts([])
      setAccountId(null)
      notifiedBuyIdsRef.current.clear()
      connectionRef.current?.stop().catch(() => undefined)
      connectionRef.current = null
      return
    }

    void refreshOpportunities()
    void refreshPositions()

    const connection = new HubConnectionBuilder()
      .withUrl(`${apiBaseUrl}/hubs/paper-trading`, { accessTokenFactory: () => getToken() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    const onPortfolioUpdated = () => { void refreshPositions() }

    connection.on('SetupDetected', onOpportunity)
    connection.on('ExitSignalDetected', onExitSignal)
    connection.on('PortfolioUpdated', onPortfolioUpdated)
    connection.on('OrderUpdated', onPortfolioUpdated)

    connection
      .start()
      .then(() => connection.invoke('SubscribeToSetups'))
      .catch(err => console.error('SignalR trade alerts failed', err))

    connectionRef.current = connection
    return () => {
      connection.off('SetupDetected', onOpportunity)
      connection.off('ExitSignalDetected', onExitSignal)
      connection.off('PortfolioUpdated', onPortfolioUpdated)
      connection.off('OrderUpdated', onPortfolioUpdated)
      connection.stop().catch(() => undefined)
    }
  }, [isAuthenticated, onOpportunity, onExitSignal, refreshOpportunities, refreshPositions])

  useEffect(() => {
    const conn = connectionRef.current
    if (!conn || conn.state !== 'Connected' || !accountId) return
    conn.invoke('SubscribeToAccount', accountId).catch(() => undefined)
  }, [accountId])

  const value = useMemo(
    () => ({
      opportunities,
      lastFullSetup,
      latestOpportunityId,
      latestOpportunity,
      opportunityVersion,
      alertActive,
      openPositions,
      exitAlerts,
      sellAlertsEnabled,
      hasOpenPosition,
      refreshOpportunities,
      refreshPositions
    }),
    [
      opportunities,
      lastFullSetup,
      latestOpportunityId,
      latestOpportunity,
      opportunityVersion,
      alertActive,
      openPositions,
      exitAlerts,
      sellAlertsEnabled,
      hasOpenPosition,
      refreshOpportunities,
      refreshPositions
    ]
  )

  return (
    <SetupNotificationContext.Provider value={value}>
      {children}
    </SetupNotificationContext.Provider>
  )
}

/** Returns live setup opportunities, positions and trade alert state. */
export function useSetupNotifications(): SetupNotificationContextValue {
  const ctx = useContext(SetupNotificationContext)
  if (!ctx) throw new Error('useSetupNotifications must be used within SetupNotificationProvider')
  return ctx
}
