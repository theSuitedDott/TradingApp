import type { UTCTimestamp, SeriesMarker, SeriesMarkerPosition } from 'lightweight-charts'
import type { ChartCandle } from '../components/CandlestickChart'
import {
  chartLayerForCondition,
  ENTRY_3PUSH_LAYER_ID,
  resolveConditionLayerId,
  SETUP_CONDITION_LAYERS
} from '../constants/setupConditionLayers'
import { isFullSetup, passedConditionCount, type SetupOpportunity } from '../types/setup'

/** A trade alert shown on the candlestick chart. */
export type ChartSignalMarker = {
  time: string
  timeUnix: UTCTimestamp
  layer: string
  kind: 'buy' | 'partial' | 'exit-sl' | 'exit-tp' | 'backtest'
  label: string
  highlighted?: boolean
}

/** Exit alert received via SignalR (not persisted on the server). */
export type ChartExitAlert = {
  id: string
  symbol: string
  reason: string
  message: string
  detectedAt: string
}

/** One backtest evaluation point with per-condition outcomes. */
export type BacktestAnalysisPoint = {
  symbol: string
  detectedAt: string
  isSetup: boolean
  confidence: number
  conditions: { name: string; passed: boolean }[]
}

/** Summary of when a condition was fulfilled in backtest results. */
export type BacktestConditionSummary = {
  layerId: string
  label: string
  color: string
  hitCount: number
  lastHitAt: string | null
}

/** Interval string to milliseconds for candle window checks. */
export function intervalToMs(interval: string): number {
  switch (interval.toLowerCase()) {
    case '15m': return 15 * 60 * 1000
    case '4h': return 4 * 60 * 60 * 1000
    case '1h':
    default: return 60 * 60 * 1000
  }
}

/** Normalizes symbols for comparison (EUR_USD, EUR/USD, EURUSD). */
export function symbolsMatch(a: string, b: string): boolean {
  return a.replace(/[_/]/g, '').toUpperCase() === b.replace(/[_/]/g, '').toUpperCase()
}

function candleUnix(candle: ChartCandle): number {
  return Math.floor(new Date(candle.time).getTime() / 1000)
}

/** Maps a detection timestamp to the nearest chart candle (null if too far outside range). */
export function candleForTime(
  isoTime: string,
  candles: ChartCandle[],
  intervalMs = 60 * 60 * 1000
): ChartCandle | null {
  if (!isoTime || candles.length === 0) return null
  const t = new Date(isoTime).getTime()
  if (Number.isNaN(t)) return null

  const firstOpen = new Date(candles[0].time).getTime()
  const lastOpen = new Date(candles[candles.length - 1].time).getTime()
  const maxSnap = Math.max(intervalMs * 2, 4 * 60 * 60 * 1000)

  if (t < firstOpen - maxSnap || t > lastOpen + maxSnap) return null

  let best = candles[0]
  let bestDiff = Math.abs(new Date(best.time).getTime() - t)
  for (const candle of candles) {
    const diff = Math.abs(new Date(candle.time).getTime() - t)
    if (diff < bestDiff) {
      bestDiff = diff
      best = candle
    }
  }

  return bestDiff <= maxSnap ? best : null
}

/** Unix timestamp aligned to a loaded chart candle (required by Lightweight Charts). */
export function candleTimeUnix(candle: ChartCandle): UTCTimestamp {
  return candleUnix(candle) as UTCTimestamp
}

/** Counts backtest points that fall inside the loaded chart window. */
export function countBacktestPointsInChartRange(
  backtestResults: BacktestAnalysisPoint[],
  chartSymbol: string,
  candles: ChartCandle[],
  interval: string
): number {
  const ms = intervalToMs(interval)
  return backtestResults.filter(
    r => symbolsMatch(r.symbol, chartSymbol) && candleForTime(r.detectedAt, candles, ms) != null
  ).length
}

function markerStyle(marker: ChartSignalMarker): Pick<SeriesMarker<UTCTimestamp>, 'shape' | 'position' | 'color'> {
  const highlighted = marker.highlighted ?? false
  switch (marker.kind) {
    case 'buy':
      return { shape: 'arrowUp', position: 'belowBar', color: highlighted ? '#34d399' : '#10b981' }
    case 'backtest':
      return { shape: 'arrowUp', position: 'belowBar', color: highlighted ? '#6ee7b7' : '#38bdf8' }
    case 'partial':
      return { shape: 'circle', position: 'aboveBar', color: highlighted ? '#fbbf24' : '#f59e0b' }
    case 'exit-sl':
      return { shape: 'arrowDown', position: 'aboveBar', color: highlighted ? '#f87171' : '#ef4444' }
    case 'exit-tp':
      return { shape: 'arrowDown', position: 'aboveBar', color: highlighted ? '#f87171' : '#ef4444' }
  }
}

/** Summarizes backtest condition hits with timestamps. */
export function summarizeBacktestConditions(
  backtestResults: BacktestAnalysisPoint[],
  chartSymbol: string
): BacktestConditionSummary[] {
  const hits = new Map<string, { count: number; lastAt: string | null }>()

  for (const layer of SETUP_CONDITION_LAYERS) {
    hits.set(layer.id, { count: 0, lastAt: null })
  }

  for (const result of backtestResults) {
    if (!symbolsMatch(result.symbol, chartSymbol)) continue
    const at = result.detectedAt
    if (!at) continue
    for (const condition of result.conditions) {
      if (!condition.passed) continue
      const layerId = resolveConditionLayerId(condition.name)
      if (!layerId) continue
      const entry = hits.get(layerId)!
      entry.count += 1
      if (!entry.lastAt || new Date(at).getTime() > new Date(entry.lastAt).getTime()) {
        entry.lastAt = at
      }
    }
  }

  return SETUP_CONDITION_LAYERS.map(layer => {
    const entry = hits.get(layer.id)!
    const roleLabel = layer.role === 'entry' ? 'Entry' : 'Exit'
    return {
      layerId: layer.id,
      label: `${roleLabel}: ${layer.label}`,
      color: layer.color,
      hitCount: entry.count,
      lastHitAt: entry.lastAt
    }
  })
}

function markerForPassedCondition(
  conditionName: string,
  candle: ChartCandle
): ChartSignalMarker | null {
  const layerId = resolveConditionLayerId(conditionName)
  if (!layerId) return null
  const layer = SETUP_CONDITION_LAYERS.find(l => l.id === layerId)
  if (!layer) return null

  const chartLayer = chartLayerForCondition(layerId)
  if (layer.role === 'entry') {
    return {
      time: candle.time,
      timeUnix: candleTimeUnix(candle),
      layer: ENTRY_3PUSH_LAYER_ID,
      kind: 'buy',
      label: 'ENTRY'
    }
  }

  return {
    time: candle.time,
    timeUnix: candleTimeUnix(candle),
    layer: chartLayer,
    kind: 'exit-tp',
    label: `EXIT ${layer.shortLabel}`
  }
}

/** Builds chart markers from live opportunities, backtest hits and exit alerts. */
export function buildChartSignalMarkers(options: {
  candles: ChartCandle[]
  chartSymbol: string
  chartInterval?: string
  opportunities: SetupOpportunity[]
  backtestResults?: BacktestAnalysisPoint[]
  exitAlerts?: ChartExitAlert[]
  highlightId?: string | null
  visibleLayers?: Record<string, boolean>
}): ChartSignalMarker[] {
  const {
    candles,
    chartSymbol,
    chartInterval = '1h',
    opportunities,
    backtestResults = [],
    exitAlerts = [],
    highlightId = null,
    visibleLayers
  } = options

  const intervalMs = intervalToMs(chartInterval)
  const markers: ChartSignalMarker[] = []

  for (const opp of opportunities) {
    if (!symbolsMatch(opp.symbol, chartSymbol)) continue
    const candle = candleForTime(opp.detectedAt, candles, intervalMs)
    if (!candle) continue

    const full = isFullSetup(opp)
    markers.push({
      time: candle.time,
      timeUnix: candleTimeUnix(candle),
      layer: full ? 'live-full' : 'live-partial',
      kind: full ? 'buy' : 'partial',
      label: full ? 'KAUF 6/6' : `Signal ${passedConditionCount(opp)}/6`,
      highlighted: opp.id === highlightId
    })
  }

  for (const result of backtestResults) {
    if (!symbolsMatch(result.symbol, chartSymbol) || !result.detectedAt) continue
    const candle = candleForTime(result.detectedAt, candles, intervalMs)
    if (!candle) continue

    if (result.isSetup) {
      markers.push({
        time: candle.time,
        timeUnix: candleTimeUnix(candle),
        layer: 'backtest-full',
        kind: 'backtest',
        label: '6/6'
      })
    } else {
      markers.push({
        time: candle.time,
        timeUnix: candleTimeUnix(candle),
        layer: 'backtest-partial',
        kind: 'partial',
        label: `${Math.round(result.confidence * 100)}%`
      })
    }

    for (const condition of result.conditions) {
      if (!condition.passed) continue
      const marker = markerForPassedCondition(condition.name, candle)
      if (marker) markers.push(marker)
    }
  }

  for (const alert of exitAlerts) {
    if (!symbolsMatch(alert.symbol, chartSymbol)) continue
    const candle = candleForTime(alert.detectedAt, candles, intervalMs)
    if (!candle) continue

    const isStop = alert.reason.toLowerCase().includes('stop')
    markers.push({
      time: candle.time,
      timeUnix: candleTimeUnix(candle),
      layer: 'exit',
      kind: isStop ? 'exit-sl' : 'exit-tp',
      label: isStop ? 'SL' : 'TP'
    })
  }

  if (!visibleLayers) return markers
  return markers.filter(marker => visibleLayers[marker.layer] !== false)
}

/** Converts domain markers to Lightweight Charts series markers (deduped by time+layer). */
export function toSeriesMarkers(
  markers: ChartSignalMarker[],
  candleTimes?: Set<number>
): SeriesMarker<UTCTimestamp>[] {
  const byKey = new Map<string, ChartSignalMarker>()

  for (const marker of markers) {
    const key = `${marker.timeUnix}|${marker.layer}`
    const existing = byKey.get(key)
    if (!existing || marker.highlighted) {
      byKey.set(key, marker)
    }
  }

  return [...byKey.values()]
    .filter(marker => !candleTimes || candleTimes.has(marker.timeUnix as number))
    .sort((a, b) => (a.timeUnix as number) - (b.timeUnix as number))
    .map(marker => {
      const style = markerStyle(marker)
      return {
        time: marker.timeUnix,
        ...style,
        text: marker.label,
        size: marker.layer === ENTRY_3PUSH_LAYER_ID ? 3 : 2
      }
    })
}
