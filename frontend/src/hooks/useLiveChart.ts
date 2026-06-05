import { useCallback, useEffect, useRef, useState } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { apiBaseUrl, getToken, setups } from '../api'
import { useToast } from '../context/ToastContext'
import type { ChartCandle } from '../components/CandlestickChart'

export type ChartLevels = {
  entry?: number
  stopLoss?: number
  takeProfit?: number
}

type LevelKey = 'entry' | 'stopLoss' | 'takeProfit'

type Options = {
  symbol: string
  exchange: string
  interval: string
  mock: boolean
  enabled: boolean
  levels?: ChartLevels
  pollMs?: number
}

function mapCandles(data: Record<string, unknown>): ChartCandle[] {
  const raw = (data.candles ?? data.Candles ?? []) as Record<string, unknown>[]
  return raw.map(c => ({
    time: String(c.time ?? c.Time),
    open: Number(c.open ?? c.Open),
    high: Number(c.high ?? c.High),
    low: Number(c.low ?? c.Low),
    close: Number(c.close ?? c.Close)
  }))
}

function extractLevels(data: Record<string, unknown>): ChartLevels | undefined {
  const entry = data.entryPrice ?? data.EntryPrice
  if (entry == null) return undefined
  return {
    entry: Number(entry),
    stopLoss: Number(data.stopLossPrice ?? data.StopLossPrice ?? 0) || undefined,
    takeProfit: Number(data.takeProfitPrice ?? data.TakeProfitPrice ?? 0) || undefined
  }
}

function crossedLevel(prev: number, next: number, level: number): boolean {
  return (prev < level && next >= level) || (prev > level && next <= level)
}

/** Loads chart candles with live updates, quote subscription and entry/exit level alerts. */
export function useLiveChart({
  symbol,
  exchange,
  interval,
  mock,
  enabled,
  levels,
  pollMs = 15_000
}: Options) {
  const { showToast } = useToast()
  const [candles, setCandles] = useState<ChartCandle[]>([])
  const [chartLevels, setChartLevels] = useState<ChartLevels | undefined>(levels)
  const [label, setLabel] = useState('')
  const [livePrice, setLivePrice] = useState<number | null>(null)
  const [isLive, setIsLive] = useState(false)
  const [loading, setLoading] = useState(false)
  const prevPriceRef = useRef<number | null>(null)
  const notifiedLevelsRef = useRef<Set<LevelKey>>(new Set())
  const connectionRef = useRef<HubConnection | null>(null)

  const checkLevelAlerts = useCallback((price: number) => {
    const prev = prevPriceRef.current
    if (prev == null || !chartLevels) {
      prevPriceRef.current = price
      return
    }

    const checks: { key: LevelKey; value?: number; title: string; message: string }[] = [
      { key: 'entry', value: chartLevels.entry, title: 'Entry erreicht', message: `${symbol}: Kurs ${price.toFixed(2)} am Einstieg.` },
      { key: 'stopLoss', value: chartLevels.stopLoss, title: 'Exit Stop-Loss', message: `${symbol}: Stop-Loss Zone ${price.toFixed(2)}.` },
      { key: 'takeProfit', value: chartLevels.takeProfit, title: 'Exit Take-Profit', message: `${symbol}: Take-Profit Ziel ${price.toFixed(2)}.` }
    ]

    for (const check of checks) {
      if (check.value == null || notifiedLevelsRef.current.has(check.key)) continue
      if (crossedLevel(prev, price, check.value)) {
        notifiedLevelsRef.current.add(check.key)
        showToast({
          title: check.title,
          message: check.message,
          variant: check.key === 'stopLoss' ? 'warning' : 'success',
          href: '/setups'
        })
      }
    }

    prevPriceRef.current = price
  }, [chartLevels, showToast, symbol])

  const applyLiveTick = useCallback((price: number) => {
    setLivePrice(price)
    setIsLive(true)
    setCandles(prev => {
      if (prev.length === 0) return prev
      const last = prev[prev.length - 1]
      const updated = {
        ...last,
        close: price,
        high: Math.max(last.high, price),
        low: Math.min(last.low, price)
      }
      return [...prev.slice(0, -1), updated]
    })
    checkLevelAlerts(price)
  }, [checkLevelAlerts])

  const refresh = useCallback(async () => {
    if (!enabled || !symbol) return
    setLoading(true)
    try {
      const data = await setups.candles({
        symbol: mock ? 'SAP' : symbol,
        interval,
        range: '60d',
        mock,
        live: true,
        includeLevels: true
      })
      const mapped = mapCandles(data ?? {})
      setCandles(mapped)
      const fromApi = extractLevels(data ?? {})
      if (fromApi) setChartLevels(fromApi)
      else if (levels) setChartLevels(levels)

      const price = data?.lastLivePrice ?? data?.LastLivePrice
      if (price != null) {
        setLivePrice(Number(price))
        setIsLive(Boolean(data?.isLive ?? data?.IsLive))
        checkLevelAlerts(Number(price))
      } else if (mapped.length > 0) {
        const lastClose = mapped[mapped.length - 1].close
        setLivePrice(lastClose)
        checkLevelAlerts(lastClose)
      }

      setLabel(
        `${data?.symbol ?? symbol} · ${data?.interval ?? interval} · ${data?.source ?? 'Chart'}` +
        (data?.isLive || data?.IsLive ? ' · LIVE' : '')
      )
    } finally {
      setLoading(false)
    }
  }, [enabled, symbol, interval, mock, levels, checkLevelAlerts])

  useEffect(() => {
    setChartLevels(levels)
    notifiedLevelsRef.current.clear()
    prevPriceRef.current = null
  }, [levels?.entry, levels?.stopLoss, levels?.takeProfit, symbol, mock])

  useEffect(() => {
    if (!enabled) return
    refresh()
    const timer = window.setInterval(refresh, pollMs)
    return () => window.clearInterval(timer)
  }, [enabled, refresh, pollMs])

  useEffect(() => {
    if (!enabled || !mock) {
      connectionRef.current?.stop().catch(() => undefined)
      connectionRef.current = null
      return
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${apiBaseUrl}/hubs/paper-trading`, { accessTokenFactory: () => getToken() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('QuoteUpdated', (tick: { symbol: string; exchange: string; price: number }) => {
      if (tick.symbol?.toUpperCase() === symbol.toUpperCase() &&
          tick.exchange?.toUpperCase() === exchange.toUpperCase()) {
        applyLiveTick(Number(tick.price))
      }
    })

    connection
      .start()
      .then(() => connection.invoke('SubscribeToQuote', symbol, exchange))
      .catch(err => console.error('Quote SignalR failed', err))

    connectionRef.current = connection
    return () => {
      connection.invoke('UnsubscribeFromQuote', symbol, exchange).catch(() => undefined)
      connection.stop().catch(() => undefined)
    }
  }, [enabled, mock, symbol, exchange, applyLiveTick])

  return {
    candles,
    chartLevels,
    label,
    livePrice,
    isLive,
    loading,
    refresh
  }
}
