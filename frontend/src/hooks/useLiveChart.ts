import { useCallback, useEffect, useRef, useState } from 'react'
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { apiBaseUrl, getToken, setups } from '../api'
import type { ChartCandle } from '../components/CandlestickChart'

export type ChartLevels = {
  entry?: number
  stopLoss?: number
  takeProfit?: number
}

type Options = {
  symbol: string
  exchange: string
  interval: string
  rangeDays: number
  mock: boolean
  enabled: boolean
  levels?: ChartLevels
}

function candlesPerDay(interval: string): number {
  switch (interval.toLowerCase()) {
    case '15m':
      return 96
    case '4h':
      return 6
    case '1h':
    default:
      return 24
  }
}

/** Returns interval duration in seconds. */
function intervalToSeconds(interval: string): number {
  switch (interval.toLowerCase()) {
    case '15m': return 15 * 60
    case '4h':  return 4 * 60 * 60
    case '1h':
    default:    return 60 * 60
  }
}

/** Returns the UTC start timestamp (seconds) of the current candle period. */
function currentPeriodStart(intervalSeconds: number): number {
  return Math.floor(Date.now() / 1000 / intervalSeconds) * intervalSeconds
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

/**
 * Chart pipeline:
 * 1. Yahoo → last 1000 candles (initial load)
 * 2. Display chart
 * 3. OANDA live ticks via SignalR
 * 4. Update current candle on each tick
 * Buy/sell toasts are handled globally in SetupNotificationContext.
 */
export function useLiveChart({
  symbol,
  exchange,
  interval,
  rangeDays,
  mock,
  enabled,
  levels
}: Options) {
  const [candles, setCandles] = useState<ChartCandle[]>([])
  const [chartLevels, setChartLevels] = useState<ChartLevels | undefined>(levels)
  const [label, setLabel] = useState('')
  const [livePrice, setLivePrice] = useState<number | null>(null)
  const [isLive, setIsLive] = useState(false)
  const [loading, setLoading] = useState(false)
  const connectionRef = useRef<HubConnection | null>(null)
  const latestCloseRef = useRef<number | null>(null)

  /** Merges a live tick into the current candle or opens a new one if the interval has rolled. */
  const applyLiveTick = useCallback((price: number) => {
    const lastClose = latestCloseRef.current
    if (lastClose != null) {
      const deviation = Math.abs(price - lastClose) / Math.max(Math.abs(lastClose), 0.000001)
      if (deviation > 0.02) {
        console.warn('Ignoring outlier live tick for chart', { symbol, price, lastClose })
        return
      }
    }

    setLivePrice(price)
    setIsLive(true)
    latestCloseRef.current = price

    const intervalSec = intervalToSeconds(interval)
    const periodStart = currentPeriodStart(intervalSec)

    setCandles(prev => {
      if (prev.length === 0) return prev
      const last = prev[prev.length - 1]
      const lastTimeSec = Math.floor(new Date(last.time).getTime() / 1000)

      if (periodStart > lastTimeSec) {
        // New interval period → open a fresh candle
        const newCandle: ChartCandle = {
          time: new Date(periodStart * 1000).toISOString(),
          open: price,
          high: price,
          low: price,
          close: price
        }
        return [...prev, newCandle]
      }

      // Still in the same period → update last candle
      const updated = {
        ...last,
        close: price,
        high: Math.max(last.high, price),
        low: Math.min(last.low, price)
      }
      return [...prev.slice(0, -1), updated]
    })
  }, [symbol, interval])

  /** Loads the Yahoo historical baseline and merges initial OANDA tick if available. */
  const loadHistorical = useCallback(async (force = false) => {
    if ((!enabled && !force) || !symbol) return
    setLoading(true)
    try {
      const days = Math.max(1, rangeDays)
      const data = await setups.candles({
        symbol: mock ? 'SAP' : symbol,
        interval,
        range: `${days}d`,
        count: days * candlesPerDay(interval),
        mock,
        live: true,
        includeLevels: true
      })
      const mapped = mapCandles(data ?? {})
      if (mapped.length === 0) {
        throw new Error('Keine Kerzendaten vom Server erhalten.')
      }
      setCandles(mapped)
      latestCloseRef.current = mapped[mapped.length - 1].close
      const fromApi = extractLevels(data ?? {})
      if (fromApi) setChartLevels(fromApi)
      else if (levels) setChartLevels(levels)

      const price = data?.lastLivePrice ?? data?.LastLivePrice
      if (price != null) {
        setLivePrice(Number(price))
        setIsLive(Boolean(data?.isLive ?? data?.IsLive))
      } else if (mapped.length > 0) {
        setLivePrice(mapped[mapped.length - 1].close)
      }

      setLabel(
        `${data?.symbol ?? symbol} · ${data?.interval ?? interval} · ${data?.source ?? 'Chart'}` +
        (data?.isLive || data?.IsLive ? ' · LIVE' : '')
      )
    } catch (ex) {
      console.error('Chart historical load failed', ex)
      throw ex
    } finally {
      setLoading(false)
    }
  }, [enabled, symbol, interval, rangeDays, mock, levels])

  useEffect(() => {
    setChartLevels(levels)
  }, [levels?.entry, levels?.stopLoss, levels?.takeProfit, symbol, mock])

  useEffect(() => {
    if (!enabled) return
    loadHistorical()
  }, [enabled, loadHistorical])

  const subscribeLiveQuotes = mock || exchange.toUpperCase() === 'OANDA'

  useEffect(() => {
    if (!enabled || !subscribeLiveQuotes) {
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
  }, [enabled, subscribeLiveQuotes, symbol, exchange, applyLiveTick])

  return {
    candles,
    chartLevels,
    label,
    livePrice,
    isLive,
    loading,
    refresh: loadHistorical
  }
}
