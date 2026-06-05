import React, { useEffect, useRef } from 'react'
import { createChart, IChartApi, IPriceLine, ISeriesApi, CandlestickData, UTCTimestamp, SeriesMarker } from 'lightweight-charts'

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
  height?: number
  livePrice?: number | null
}

function toUtc(time: string): UTCTimestamp {
  return Math.floor(new Date(time).getTime() / 1000) as UTCTimestamp
}

export default function CandlestickChart({ candles, levels, height = 420, livePrice }: Props) {
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
      timeScale: { borderColor: '#374151', timeVisible: true, secondsVisible: false }
    })

    const series = chart.addCandlestickSeries({
      upColor: '#10b981',
      downColor: '#ef4444',
      borderVisible: false,
      wickUpColor: '#10b981',
      wickDownColor: '#ef4444'
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
      open: c.open,
      high: c.high,
      low: c.low,
      close: c.close
    }))

    if (lastCandleCountRef.current > 0 && data.length === lastCandleCountRef.current) {
      series.update(data[data.length - 1])
    } else {
      series.setData(data)
      chartRef.current?.timeScale().fitContent()
    }

    lastCandleCountRef.current = data.length
  }, [candles])

  useEffect(() => {
    const series = seriesRef.current
    if (!series) return

    priceLinesRef.current.forEach(line => series.removePriceLine(line))
    priceLinesRef.current = []

    const addLine = (price: number, color: string, title: string) => {
      priceLinesRef.current.push(series.createPriceLine({
        price,
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

  useEffect(() => {
    const series = seriesRef.current
    if (!series || candles.length === 0) return

    const markerTime = toUtc(candles[candles.length - 1].time)
    const markers: SeriesMarker<UTCTimestamp>[] = []

    if (levels?.entry != null) {
      markers.push({
        time: markerTime,
        position: 'belowBar',
        color: '#3b82f6',
        shape: 'arrowUp',
        text: 'Entry'
      })
    }
    if (levels?.stopLoss != null) {
      markers.push({
        time: markerTime,
        position: 'aboveBar',
        color: '#ef4444',
        shape: 'arrowDown',
        text: 'Exit SL'
      })
    }
    if (levels?.takeProfit != null) {
      markers.push({
        time: markerTime,
        position: 'aboveBar',
        color: '#10b981',
        shape: 'circle',
        text: 'Exit TP'
      })
    }

    series.setMarkers(markers)
  }, [candles, levels])

  return (
    <div
      ref={containerRef}
      className="w-full rounded border border-gray-700 overflow-hidden"
      style={{ minHeight: height }}
    />
  )
}
