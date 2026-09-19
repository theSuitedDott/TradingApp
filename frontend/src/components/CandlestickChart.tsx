import React, { useEffect, useRef } from 'react'
import { createChart, IChartApi, IPriceLine, ISeriesApi, CandlestickData, UTCTimestamp, TickMarkType } from 'lightweight-charts'
import { type ChartSignalMarker, toSeriesMarkers } from '../utils/chartSignalMarkers'

export type ChartCandle = {
  time: string
  open: number
  high: number
  low: number
  close: number
}

type Levels = {
  entry?: number
  stopLoss?: number
  takeProfit?: number
}

type Props = {
  candles: ChartCandle[]
  levels?: Levels
  signalMarkers?: ChartSignalMarker[]
  height?: number
  livePrice?: number | null
}

function toUtc(time: string): UTCTimestamp {
  return Math.floor(new Date(time).getTime() / 1000) as UTCTimestamp
}

const BERLIN_TZ = 'Europe/Berlin'

function formatBerlinTime(unixSeconds: number, tickMarkType: TickMarkType): string {
  const date = new Date(unixSeconds * 1000)
  if (tickMarkType === TickMarkType.Time) {
    return date.toLocaleTimeString('de-DE', { timeZone: BERLIN_TZ, hour: '2-digit', minute: '2-digit' })
  }
  if (tickMarkType === TickMarkType.DayOfMonth) {
    return date.toLocaleDateString('de-DE', { timeZone: BERLIN_TZ, day: '2-digit', month: '2-digit' })
  }
  if (tickMarkType === TickMarkType.Month) {
    return date.toLocaleDateString('de-DE', { timeZone: BERLIN_TZ, month: 'short', year: '2-digit' })
  }
  return date.toLocaleDateString('de-DE', { timeZone: BERLIN_TZ, year: 'numeric' })
}

function roundPrice(value: number): number {
  return Math.round(value * 10000) / 10000
}

export default function CandlestickChart({ candles, levels, signalMarkers = [], height = 420, livePrice }: Props) {
  const containerRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<IChartApi | null>(null)
  const seriesRef = useRef<ISeriesApi<'Candlestick'> | null>(null)
  const priceLinesRef = useRef<IPriceLine[]>([])
  const lastCandleCountRef = useRef(0)

  useEffect(() => {
    if (!containerRef.current) return

    const chart = createChart(containerRef.current, {
      width: containerRef.current.clientWidth,
      height,
      layout: {
        background: { color: '#111827' },
        textColor: '#d1d5db'
      },
      grid: {
        vertLines: { color: '#1f2937' },
        horzLines: { color: '#1f2937' }
      },
      crosshair: { mode: 1 },
      rightPriceScale: { borderColor: '#374151' },
      localization: {
        timeFormatter: (unixSeconds: UTCTimestamp) => {
          const date = new Date(unixSeconds * 1000)
          return date.toLocaleString('de-DE', {
            timeZone: BERLIN_TZ,
            day: '2-digit', month: '2-digit',
            hour: '2-digit', minute: '2-digit'
          })
        }
      },
      timeScale: {
        borderColor: '#374151',
        timeVisible: true,
        secondsVisible: false,
        tickMarkFormatter: (time: UTCTimestamp, tickMarkType: TickMarkType) =>
          formatBerlinTime(time as number, tickMarkType)
      }
    })

    const series = chart.addCandlestickSeries({
      upColor: '#10b981',
      downColor: '#ef4444',
      borderVisible: false,
      wickUpColor: '#10b981',
      wickDownColor: '#ef4444',
      priceFormat: {
        type: 'price',
        precision: 4,
        minMove: 0.0001
      }
    })

    chartRef.current = chart
    seriesRef.current = series

    const onResize = () => {
      if (containerRef.current && chartRef.current) {
        chartRef.current.applyOptions({ width: containerRef.current.clientWidth })
      }
    }
    window.addEventListener('resize', onResize)

    return () => {
      window.removeEventListener('resize', onResize)
      chart.remove()
      chartRef.current = null
      seriesRef.current = null
    }
  }, [height])

  useEffect(() => {
    const series = seriesRef.current
    if (!series || candles.length === 0) return

    const data: CandlestickData[] = candles.map(c => ({
      time: toUtc(c.time),
      open: roundPrice(c.open),
      high: roundPrice(c.high),
      low: roundPrice(c.low),
      close: roundPrice(c.close)
    }))

    const candleTimes = new Set(data.map(d => d.time as number))
    const shouldFitContent = data.length !== lastCandleCountRef.current
    series.setData(data)
    lastCandleCountRef.current = data.length

    if (shouldFitContent) {
      chartRef.current?.timeScale().fitContent()
    }
  }, [candles])

  useEffect(() => {
    const series = seriesRef.current
    if (!series || candles.length === 0) return
    const candleTimes = new Set(candles.map(c => toUtc(c.time) as number))
    series.setMarkers(toSeriesMarkers(signalMarkers, candleTimes))
  }, [candles, signalMarkers])

  useEffect(() => {
    const series = seriesRef.current
    if (!series) return

    priceLinesRef.current.forEach(line => series.removePriceLine(line))
    priceLinesRef.current = []

    const addLine = (price: number, color: string, title: string) => {
      priceLinesRef.current.push(series.createPriceLine({
        price: roundPrice(price),
        color,
        lineWidth: 2,
        title,
        axisLabelVisible: true
      }))
    }

    if (levels?.entry != null) addLine(levels.entry, '#3b82f6', 'Entry')
    if (levels?.stopLoss != null) addLine(levels.stopLoss, '#ef4444', 'Exit SL')
    if (levels?.takeProfit != null) addLine(levels.takeProfit, '#10b981', 'Exit TP')
    if (livePrice != null) {
      addLine(livePrice, '#f59e0b', 'Live')
    }
  }, [levels, livePrice])

  return (
    <div
      ref={containerRef}
      className="w-full rounded border border-gray-700 overflow-hidden"
      style={{ minHeight: height }}
    />
  )
}
