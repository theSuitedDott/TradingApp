import React, { useEffect, useMemo, useRef, useState } from 'react'
import { accounts, setups } from '../api'
import { useSetupNotifications } from '../context/SetupNotificationContext'
import { SETUP_DISPLAY_LIMIT_OPTIONS, useSetupDisplayLimit } from '../hooks/useSetupDisplayLimit'
import { isFullSetup, normalizeOpportunity, passedConditionCount, type SetupOpportunity } from '../types/setup'
import {
  CHART_SIGNAL_LAYERS,
  createDefaultLayerVisibility,
  ENTRY_3PUSH_LAYER_ID,
  EXIT_LAYER_PREFIX,
  SETUP_CONDITION_LAYERS
} from '../constants/setupConditionLayers'
import {
  buildChartSignalMarkers,
  countBacktestPointsInChartRange,
  summarizeBacktestConditions
} from '../utils/chartSignalMarkers'
import CandlestickChart from '../components/CandlestickChart'
import { useLiveChart, type ChartLevels } from '../hooks/useLiveChart'
import InfoCallout from '../components/InfoCallout'
import LabelWithTooltip from '../components/LabelWithTooltip'
import SectionCard from '../components/SectionCard'
import Tooltip from '../components/Tooltip'

const CONDITION_GLOSSARY = [
  { name: 'Klarer H4-Trend', text: 'Auf dem 4-Stunden-Chart müssen höhere Hochs und höhere Tiefs (Long) bzw. tiefere Hochs und Tiefs (Short) erkennbar sein. Ohne klaren Trend gibt es kein Setup.' },
  { name: '3-Push (Einstieg am 3. Push, Ziel vorheriger Peak)', text: 'Drei abnehmende Korrektur-Bewegungen gegen den Trend. Kauf/Verkauf am 3. Push; Take-Profit am letzten Swing-Hoch/Tief vor der Korrektur (Gewinnzone bis zum vorherigen Peak).' },
  { name: 'Liquiditäts-Sweep (Inducement)', text: 'Der letzte Push durchbricht kurz ein vorheriges Hoch/Tief (Liquiditätszone), holt Stop-Loss-Orders ab und schließt wieder zurück — ein typisches „Shake-out“.' },
  { name: 'Displacement-Kerze', text: 'Eine starke Kerze zurück in Trendrichtung. Zeigt, dass institutionelle Orders den Markt wieder übernehmen.' },
  { name: 'Fair Value Gap (Einstieg)', text: 'Eine Preislücke (Imbalance) aus drei Kerzen. Der Einstieg erfolgt in dieser Zone — nicht blind am Marktpreis.' },
  { name: 'DXY/VIX-Bestätigung', text: 'Dollar-Index und Volatilitätsindex müssen die Richtung stützen. Long: beide fallen (Risk-on). Short: beide steigen (Risk-off).' }
]

type Condition = { name: string; passed: boolean; detail: string }
type Opportunity = SetupOpportunity
type Analysis = {
  symbol: string
  exchange: string
  bias: string
  confidence: number
  isSetup: boolean
  detectedAt: string
  entryPrice?: number
  stopLossPrice?: number
  takeProfitPrice?: number
  conditions: Condition[]
  opportunity: Opportunity | null
}

function numOrUndef(v: unknown): number | undefined {
  if (v == null || v === '') return undefined
  const n = Number(v)
  return Number.isFinite(n) ? n : undefined
}

function isThreePushCondition(name: string): boolean {
  return /3-Push|3 Push|Erschöpfung/i.test(name)
}

function levelsFromAnalysis(res: Analysis): ChartLevels | undefined {
  if (res.entryPrice == null || res.takeProfitPrice == null) return undefined
  return {
    entry: res.entryPrice,
    stopLoss: res.stopLossPrice,
    takeProfit: res.takeProfitPrice
  }
}

function normalizeAnalysis(raw: Record<string, unknown>): Analysis {
  const oppRaw = raw.opportunity ?? raw.Opportunity
  const opp = oppRaw ? normalizeOpportunity(oppRaw as Record<string, unknown>) : null
  const conditionsRaw = (raw.conditions ?? raw.Conditions ?? []) as Record<string, unknown>[]

  return {
    symbol: String(raw.symbol ?? raw.Symbol ?? ''),
    exchange: String(raw.exchange ?? raw.Exchange ?? ''),
    bias: String(raw.bias ?? raw.Bias ?? ''),
    confidence: Number(raw.confidence ?? raw.Confidence ?? 0),
    isSetup: Boolean(raw.isSetup ?? raw.IsSetup),
    detectedAt: String(
      raw.detectedAt ?? raw.DetectedAt ?? opp?.detectedAt ?? ''
    ),
    entryPrice: numOrUndef(raw.entryPrice ?? raw.EntryPrice ?? opp?.entryPrice),
    stopLossPrice: numOrUndef(raw.stopLossPrice ?? raw.StopLossPrice ?? opp?.stopLossPrice),
    takeProfitPrice: numOrUndef(raw.takeProfitPrice ?? raw.TakeProfitPrice ?? opp?.takeProfitPrice),
    conditions: conditionsRaw.map(c => ({
      name: String(c.name ?? c.Name ?? ''),
      passed: Boolean(c.passed ?? c.Passed),
      detail: String(c.detail ?? c.Detail ?? '')
    })),
    opportunity: opp
  }
}

export default function Setups() {
  const {
    opportunities,
    lastFullSetup,
    latestOpportunity,
    opportunityVersion,
    exitAlerts,
    sellAlertsEnabled,
    hasOpenPosition,
    refreshOpportunities,
    refreshPositions
  } = useSetupNotifications()
  const { limit: displayLimit, setLimit: setDisplayLimit, applyLimit } = useSetupDisplayLimit()
  const opportunitiesRef = useRef<HTMLElement | null>(null)
  const [highlightId, setHighlightId] = useState<string | null>(null)
  const [newTradeNotice, setNewTradeNotice] = useState<string | null>(null)
  const [analysis, setAnalysis] = useState<Analysis | null>(null)
  const [accountsList, setAccountsList] = useState<any[]>([])
  const [accountId, setAccountId] = useState<string>('')
  const [quantity, setQuantity] = useState<number>(10)
  const [banner, setBanner] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [backtestResults, setBacktestResults] = useState<Analysis[]>([])
  const [backtestSymbol, setBacktestSymbol] = useState<string>('EUR_USD')
  const [isBacktesting, setIsBacktesting] = useState(false)
  const [isCheckingExits, setIsCheckingExits] = useState(false)
  const [chartInterval, setChartInterval] = useState<string>('15m')
  const [chartRangeDays, setChartRangeDays] = useState<number>(2)
  const [chartMock, setChartMock] = useState(false)
  const [chartEnabled] = useState(true)
  const [manualLevels, setManualLevels] = useState<ChartLevels | undefined>()
  const [visibleMarkerLayers, setVisibleMarkerLayers] = useState(createDefaultLayerVisibility)

  const chartSymbol = chartMock ? 'SAP' : backtestSymbol
  const chartExchange = chartMock ? 'XETRA' : 'OANDA'

  const backtestPoints = useMemo(
    () => backtestResults.map(res => ({
      symbol: res.symbol,
      detectedAt: res.detectedAt,
      isSetup: res.isSetup,
      confidence: res.confidence,
      conditions: res.conditions
    })),
    [backtestResults]
  )

  const backtestConditionSummary = useMemo(
    () => summarizeBacktestConditions(backtestPoints, chartSymbol),
    [backtestPoints, chartSymbol]
  )

  /** Liste: nur starke Setups (4/6+). Chart: alle Bedingungs-Treffer. */
  const backtestListResults = useMemo(
    () => backtestResults.filter(r => r.isSetup || r.confidence >= 0.66),
    [backtestResults]
  )

  const {
    candles: chartCandles,
    chartLevels,
    label: chartLabel,
    livePrice,
    isLive,
    loading: isLoadingChart,
    refresh: refreshChart
  } = useLiveChart({
    symbol: chartSymbol,
    exchange: chartExchange,
    interval: chartInterval,
    rangeDays: chartRangeDays,
    mock: chartMock,
    enabled: chartEnabled,
    levels: manualLevels
  })

  const chartSignalMarkers = useMemo(
    () => buildChartSignalMarkers({
      candles: chartCandles,
      chartSymbol,
      chartInterval,
      opportunities,
      backtestResults: backtestPoints,
      exitAlerts,
      highlightId,
      visibleLayers: visibleMarkerLayers
    }),
    [chartCandles, chartSymbol, chartInterval, opportunities, backtestPoints, exitAlerts, highlightId, visibleMarkerLayers]
  )

  const backtestPointsInChart = useMemo(
    () => countBacktestPointsInChartRange(backtestPoints, chartSymbol, chartCandles, chartInterval),
    [backtestPoints, chartSymbol, chartCandles, chartInterval]
  )

  function toggleMarkerLayer(layerId: string) {
    setVisibleMarkerLayers(prev => ({ ...prev, [layerId]: !prev[layerId] }))
  }

  useEffect(() => {
    setError(null)
    refreshOpportunities().catch((ex: Error) => setError(ex.message))
    loadAccounts()
  }, [refreshOpportunities])

  useEffect(() => {
    if (lastFullSetup) setBanner(lastFullSetup.message)
  }, [lastFullSetup])

  useEffect(() => {
    if (opportunityVersion === 0 || !latestOpportunity) return

    let cancelled = false
    ;(async () => {
      await refreshOpportunities()
      if (cancelled) return

      const latest = latestOpportunity
      const score = passedConditionCount(latest)
      setBanner(latest.message)
      setNewTradeNotice(
        isFullSetup(latest)
          ? `Neuer Trade: ${latest.symbol} ${latest.direction} — alle 6 Bedingungen erfüllt!`
          : `Neues Signal: ${latest.symbol} (${score}/6 Bedingungen)`
      )
      setHighlightId(latest.id)

      if (isFullSetup(latest)) {
        setManualLevels({
          entry: latest.entryPrice,
          stopLoss: latest.stopLossPrice,
          takeProfit: latest.takeProfitPrice
        })
      }

      opportunitiesRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
      window.setTimeout(() => {
        if (!cancelled) setHighlightId(current => (current === latest.id ? null : current))
      }, 10_000)
    })()

    return () => {
      cancelled = true
    }
  }, [opportunityVersion, latestOpportunity, refreshOpportunities, refreshChart])

  const visibleOpportunities = applyLimit(opportunities)
  const hiddenCount = displayLimit > 0 ? Math.max(0, opportunities.length - displayLimit) : 0

  async function loadAccounts() {
    try {
      const accs = await accounts.list()
      setAccountsList(accs ?? [])
      if (accs?.length > 0) setAccountId(accs[0].id)
    } catch {
      setAccountsList([])
    }
  }

  async function startChart() {
    setError(null)
    try {
      await refreshChart(true)
    } catch (ex: any) {
      setError(ex?.message ?? 'Chart konnte nicht aktualisiert werden.')
    }
  }

  async function runAnalysis() {
    setError(null)
    try {
      const result = await setups.analyze()
      setAnalysis(result)
      setChartMock(true)
      if (result.opportunity) {
        setManualLevels({
          entry: result.opportunity.entryPrice,
          stopLoss: result.opportunity.stopLossPrice,
          takeProfit: result.opportunity.takeProfitPrice
        })
      }
    } catch (ex: any) {
      setError(ex.message)
    }
  }

  async function checkExitsNow() {
    setError(null)
    setIsCheckingExits(true)
    try {
      const alerts = await setups.checkExits()
      if (Array.isArray(alerts) && alerts.length > 0) {
        setBanner(`${alerts.length} Verkaufssignal(e): SL/TP erreicht — Details in der Toast-Meldung.`)
      } else {
        setBanner('Verkaufs-Check: Kein Signal — Stop-Loss und Take-Profit noch nicht erreicht.')
      }
      await refreshPositions()
    } catch (ex: any) {
      setError(ex?.message ?? 'Verkaufs-Check fehlgeschlagen.')
    } finally {
      setIsCheckingExits(false)
    }
  }

  async function runBacktest() {
    setError(null)
    setIsBacktesting(true)
    setBacktestResults([])
    try {
      const results = await setups.backtest(backtestSymbol)
      if (Array.isArray(results)) {
        const normalized = results.map((r: Record<string, unknown>) => normalizeAnalysis(r))
        setBacktestResults(normalized)
        setChartMock(false)
        setChartRangeDays(60)
        setChartInterval('1h')
        const latestThreePush = [...normalized].reverse().find(
          r => r.conditions.some(c => c.passed && isThreePushCondition(c.name))
        )
        setManualLevels(latestThreePush ? levelsFromAnalysis(latestThreePush) : undefined)
        const withTime = normalized.filter(r => r.detectedAt).length
        const threePushCount = normalized.filter(
          r => r.conditions.some(c => c.passed && isThreePushCondition(c.name))
        ).length
        if (results.length === 0) {
          setBanner(`Backtest für ${backtestSymbol}: Keine Bedingungen erfüllt in den letzten 60 Tagen. Backend neu starten, falls du kürzlich aktualisiert hast.`)
        } else {
          setBanner(
            `Backtest: ${results.length} Bewertungen, ${threePushCount}× 3-Push — Chart 60T/H1. Linien: Einstieg am 3. Push, TP am vorherigen Peak.`
          )
        }
      } else {
        setError(results || 'Fehler beim Backtest')
      }
    } catch (ex: any) {
      setError(ex.message)
    } finally {
      setIsBacktesting(false)
    }
  }

  async function execute(opp: Opportunity) {
    setError(null)
    if (!accountId) {
      setError('Bitte zuerst ein Paper-Konto auswählen.')
      return
    }
    try {
      await setups.execute(opp.id, { accountId, quantity })
      setBanner(`Order für ${opp.symbol} ausgeführt. Verkaufs-Warnungen sind jetzt aktiv.`)
      await Promise.all([refreshOpportunities(), refreshPositions()])
    } catch (ex: any) {
      setError(ex.message)
    }
  }

  return (
    <div className="max-w-5xl">
      <h2 className="text-2xl font-semibold">Institutionelles Setup</h2>
      <p className="text-sm text-gray-400 mt-1">
        Analyse- und Signal-Arbeitsplatz — hier erkennst du Setups, siehst sie im Chart und führst Paper-Trades aus.
      </p>

      <div className="mt-4 space-y-4">
        <InfoCallout title="Kauf- & Verkaufs-Warnungen">
          <p>
            <strong>KAUF-SIGNAL</strong> (Toast + Tab-Blink): nur bei 6/6 Setup und{' '}
            <em>ohne</em> offene Position im gleichen Symbol — kein Dauer-Alarm nach dem Kauf.
          </p>
          <p>
            <strong>VERKAUF-SIGNAL</strong>: Scanner prüft jede <strong>60 Sekunden</strong> offene Positionen
            gegen Stop-Loss / Take-Profit — oder sofort per Button „Verkauf jetzt prüfen“.
          </p>
          <p className="text-gray-500 text-xs mt-1">
            Status: {sellAlertsEnabled ? 'Verkaufs-Warnungen aktiv' : 'Nur Kauf-Warnungen (noch keine offene Position)'}
            {hasOpenPosition(backtestSymbol) ? ` · Position offen: ${backtestSymbol}` : ''}
          </p>
        </InfoCallout>

        <InfoCallout title="Warum dieser Reiter existiert (und nicht im Dashboard)">
          <p>
            <strong>Dashboard</strong> = Portfolio-Übersicht (Konten, Positionen, manuelle Orders).{' '}
            <strong>Setups</strong> = strategische Analyse (6 Bedingungen prüfen, Chart ansehen, Backtest, Trade auslösen).
          </p>
          <p>
            So bleibt klar getrennt: <em>Was soll ich handeln?</em> (hier) vs. <em>Wie steht mein Konto?</em> (Dashboard/Paper Trading).
          </p>
        </InfoCallout>

        <details className="card text-sm">
          <summary className="cursor-pointer font-medium text-gray-200">
            Die 6 Bedingungen im Detail (Glossar)
          </summary>
          <ul className="mt-3 space-y-3">
            {CONDITION_GLOSSARY.map(c => (
              <li key={c.name}>
                <strong className="text-gray-100">{c.name}</strong>
                <p className="text-gray-400 mt-0.5">{c.text}</p>
              </li>
            ))}
          </ul>
        </details>
      </div>

      {banner && (
        <div className="mt-4 card border border-emerald-600 bg-emerald-900/30 text-emerald-200">
          {banner}
        </div>
      )}
      {error && (
        <div className="mt-4 card border border-red-600 bg-red-900/30 text-red-200">{error}</div>
      )}

      <SectionCard
        className="mt-4"
        title="Kerzenchart"
        description="Yahoo liefert den gewählten Zeitraum, standardmäßig 2 Tage mit 15-Minuten-Kerzen. OANDA-Live-Ticks aktualisieren nur die aktuelle Kerze."
        tooltip="Ablauf: Yahoo (gewählter Zeitraum) → Chart → OANDA Live-Tick → letzte Kerze. Für 2 Tage/15m werden ca. 192 Kerzen geladen."
      >
        <div className="flex flex-wrap items-end gap-4">
          <div>
            <LabelWithTooltip
              label="Chart-Symbol"
              tooltip="EUR/USD oder GBP/USD über OANDA. Bei Mock-Daten wird immer SAP gezeigt."
            />
            <select
              className="bg-gray-900 border border-gray-700 rounded px-3 py-2 mt-1"
              value={backtestSymbol}
              onChange={e => setBacktestSymbol(e.target.value)}
              disabled={chartMock}
            >
              <option value="EUR_USD">EUR/USD</option>
              <option value="GBP_USD">GBP/USD</option>
            </select>
          </div>
          <div>
            <LabelWithTooltip
              label="Timeframe"
              tooltip="Für Backtest-Markierungen: H1 (wie der Algorithmus). H4 nicht nötig. M15 nur für feines Intraday."
            />
            <select
              className="bg-gray-900 border border-gray-700 rounded px-3 py-2 mt-1"
              value={chartInterval}
              onChange={e => setChartInterval(e.target.value)}
            >
              <option value="15m">M15 (15 Minuten)</option>
              <option value="1h">H1 (Einstieg)</option>
              <option value="4h">H4 (Trend)</option>
            </select>
          </div>
          <div>
            <LabelWithTooltip
              label="Zeitraum"
              tooltip="Backtest braucht mindestens 14 Tage sichtbar. Nach Backtest wird automatisch auf 14 Tage / H1 umgestellt."
            />
            <select
              className="bg-gray-900 border border-gray-700 rounded px-3 py-2 mt-1"
              value={chartRangeDays}
              onChange={e => setChartRangeDays(Number(e.target.value))}
            >
              <option value={1}>1 Tag</option>
              <option value={2}>2 Tage</option>
              <option value={5}>5 Tage</option>
              <option value={10}>10 Tage</option>
              <option value={14}>14 Tage (Backtest)</option>
              <option value={30}>30 Tage</option>
            </select>
          </div>
          <div className="flex items-center gap-2 mt-5">
            <input
              id="chartMock"
              type="checkbox"
              checked={chartMock}
              onChange={e => setChartMock(e.target.checked)}
              className="rounded"
            />
            <label htmlFor="chartMock" className="text-sm text-gray-300">
              <Tooltip text="Demo-Kerzen mit perfektem Lehrbuch-Setup für SAP. Ideal zum Verstehen der Strategie — nicht für echtes Trading.">
                Mock-Daten (SAP)
              </Tooltip>
            </label>
          </div>
          <button
            className="btn-primary"
            disabled={isLoadingChart}
            title="Startet Live-Chart mit Entry/Exit-Levels."
            onClick={startChart}
          >
            {isLoadingChart ? 'Lade Chart...' : 'Chart aktualisieren'}
          </button>
        </div>

        {chartLabel && (
          <p className="text-xs text-gray-400 mt-3">
            {chartLabel} · {chartCandles.length} Kerzen
            {isLive && livePrice != null && (
              <span className="ml-2 text-amber-400 font-mono">Live: {livePrice.toFixed(4)}</span>
            )}
          </p>
        )}

        {chartLevels && (
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-2 mt-3 text-xs">
            {chartLevels.entry != null && (
              <div className="p-2 rounded bg-blue-950/30 border border-blue-800/40 text-blue-200">
                Entry: <span className="font-mono">{chartLevels.entry.toFixed(4)}</span>
              </div>
            )}
            {chartLevels.stopLoss != null && (
              <div className="p-2 rounded bg-red-950/30 border border-red-800/40 text-red-200">
                Exit SL: <span className="font-mono">{chartLevels.stopLoss.toFixed(4)}</span>
              </div>
            )}
            {chartLevels.takeProfit != null && (
              <div className="p-2 rounded bg-emerald-950/30 border border-emerald-800/40 text-emerald-200">
                Exit TP: <span className="font-mono">{chartLevels.takeProfit.toFixed(4)}</span>
              </div>
            )}
          </div>
        )}

        {chartCandles.length > 0 ? (
          <div className="mt-3">
            <div className="mb-3 p-3 rounded border border-gray-700 bg-gray-900/50">
              <p className="text-xs font-semibold text-gray-300 mb-2">Markierungen im Chart</p>
              <div className="flex flex-wrap gap-x-4 gap-y-2 text-xs">
                {CHART_SIGNAL_LAYERS.map(layer => (
                  <label key={layer.id} className="inline-flex items-center gap-1.5 cursor-pointer text-gray-300">
                    <input
                      type="checkbox"
                      checked={visibleMarkerLayers[layer.id] !== false}
                      onChange={() => toggleMarkerLayer(layer.id)}
                    />
                    <span className="inline-block w-2 h-2 rounded-full" style={{ backgroundColor: layer.color }} />
                    {layer.label}
                  </label>
                ))}
              </div>
              {backtestResults.length > 0 ? (
                <>
                  <p className="text-xs font-semibold text-gray-300 mt-3 mb-2">
                    Backtest-Marker — Entry (3-Push) &amp; Exit (übrige Bedingungen)
                  </p>
                  <p className="text-xs text-gray-500 mb-2">
                    Grüner Pfeil nach oben = Entry am 3-Push. Rote Pfeile nach unten = Exit-Signale (Sweep, FVG, …).
                  </p>
                  <div className="flex flex-wrap gap-x-4 gap-y-2 text-xs">
                    <label className="inline-flex items-center gap-1.5 cursor-pointer text-gray-300">
                      <input
                        type="checkbox"
                        checked={visibleMarkerLayers[ENTRY_3PUSH_LAYER_ID] !== false}
                        onChange={() => toggleMarkerLayer(ENTRY_3PUSH_LAYER_ID)}
                      />
                      <span className="inline-block w-2 h-2 rounded-full bg-emerald-500" />
                      Entry (3-Push)
                    </label>
                    {SETUP_CONDITION_LAYERS.filter(l => l.role === 'exit').map(layer => (
                      <label key={layer.id} className="inline-flex items-center gap-1.5 cursor-pointer text-gray-300">
                        <input
                          type="checkbox"
                          checked={visibleMarkerLayers[EXIT_LAYER_PREFIX + layer.id] !== false}
                          onChange={() => toggleMarkerLayer(EXIT_LAYER_PREFIX + layer.id)}
                        />
                        <span className="inline-block w-2 h-2 rounded-full bg-red-500" />
                        Exit {layer.shortLabel}
                      </label>
                    ))}
                  </div>
                  <div className="mt-3 overflow-x-auto">
                    <table className="w-full text-xs text-left text-gray-400">
                      <thead>
                        <tr className="text-gray-500 border-b border-gray-700">
                          <th className="py-1 pr-3">Bedingung</th>
                          <th className="py-1 pr-3">Treffer</th>
                          <th className="py-1">Letzter Treffer (Berlin)</th>
                        </tr>
                      </thead>
                      <tbody>
                        {backtestConditionSummary.map(row => (
                          <tr key={row.layerId} className="border-b border-gray-800/60">
                            <td className="py-1 pr-3">
                              <span className="inline-block w-2 h-2 rounded-full mr-1.5" style={{ backgroundColor: row.color }} />
                              {row.label}
                            </td>
                            <td className="py-1 pr-3 font-mono">{row.hitCount}</td>
                            <td className="py-1">
                              {row.lastHitAt
                                ? new Date(row.lastHitAt).toLocaleString('de-DE', { timeZone: 'Europe/Berlin' })
                                : '—'}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </>
              ) : (
                <p className="text-xs text-gray-500 mt-2">
                  Starte einen Backtest, um zu sehen welche Bedingungen wann erfüllt wurden.
                </p>
              )}
            </div>
            <CandlestickChart
              candles={chartCandles}
              levels={chartLevels}
              signalMarkers={chartSignalMarkers}
              livePrice={livePrice}
            />
            {backtestResults.length > 0 && backtestPointsInChart === 0 && (
              <p className="text-xs text-amber-400 mt-2">
                Backtest hat {backtestResults.length} Treffer, aber keiner liegt im aktuellen Chart-Zeitraum.
                Stelle Zeitraum auf 60 Tage und Timeframe auf H1.
              </p>
            )}
            {backtestResults.length > 0 && chartSignalMarkers.length === 0 && (
              <p className="text-xs text-amber-400 mt-2">
                {backtestResults.length} Backtest-Bewertungen, aber 0 Marker sichtbar.
                Prüfe: Zeitraum 60 Tage, H1, Bedingungs-Checkboxen aktiv? Backend neu gestartet?
                ({backtestPointsInChart} Punkte im Chart-Zeitraum)
              </p>
            )}
            {chartSignalMarkers.length > 0 && (
              <p className="text-xs text-gray-500 mt-2">
                {chartSignalMarkers.length} sichtbare Markierung{chartSignalMarkers.length === 1 ? '' : 'en'} für {chartSymbol}
                {backtestResults.length > 0 && ` (${backtestPointsInChart} Backtest-Punkte im Chart)`}.
              </p>
            )}
          </div>
        ) : (
          <p className="text-sm text-gray-500 mt-3">
            Klicke „Chart laden“ für Marktdaten — oder „Jetzt analysieren (Mock)“ für das Demo-Setup mit Trade-Levels.
          </p>
        )}
      </SectionCard>

      <SectionCard
        className="mt-4"
        title="Demo-Analyse (Mock)"
        description="Prüft alle 6 Bedingungen anhand eines konstruierten SAP-Setups — zeigt dir, wie ein perfektes Signal aussieht."
        tooltip="Nutzt keine echten Marktdaten. Dient nur zum Verstehen und Testen des Algorithmus."
      >
        <div className="flex flex-wrap items-end gap-4">
          <div>
            <LabelWithTooltip
              label="Paper-Konto"
              tooltip="Virtuelles Konto für die Ein-Klick-Order. Erstelle ein Konto unter Paper Trading, falls keines vorhanden ist."
            />
            <select
              className="bg-gray-900 border border-gray-700 rounded px-3 py-2 mt-1"
              value={accountId}
              onChange={e => setAccountId(e.target.value)}
            >
              {accountsList.length === 0 && <option value="">Kein Konto — bitte unter Paper Trading anlegen</option>}
              {accountsList.map(a => (
                <option key={a.id} value={a.id}>{a.name}</option>
              ))}
            </select>
          </div>
          <div>
            <LabelWithTooltip
              label="Menge"
              tooltip="Stückzahl/Lot-Größe für die Paper-Order, wenn du „Trade ausführen“ klickst."
            />
            <input
              type="number"
              min={1}
              step={1}
              className="bg-gray-900 border border-gray-700 rounded px-3 py-2 mt-1 w-28"
              value={quantity}
              onChange={e => {
                const raw = e.target.value
                if (raw === '') {
                  setQuantity(1)
                  return
                }
                const next = parseInt(raw, 10)
                if (!Number.isNaN(next)) setQuantity(Math.max(1, next))
              }}
            />
          </div>
          <button
            className="btn-primary"
            title="Führt die 6-Punkte-Prüfung mit Demo-Daten aus und lädt den Chart mit Entry/SL/TP."
            onClick={runAnalysis}
          >
            Jetzt analysieren (Mock)
          </button>
          <button
            type="button"
            className="btn-primary bg-amber-700 hover:bg-amber-600 disabled:opacity-50"
            title={
              sellAlertsEnabled
                ? 'Prüft sofort alle offenen Positionen gegen Stop-Loss und Take-Profit (sonst alle 60s automatisch).'
                : 'Erst relevant nach deinem ersten Kauf — dann SL/TP alle 60s oder per Klick.'
            }
            disabled={isCheckingExits}
            onClick={checkExitsNow}
          >
            {isCheckingExits ? 'Prüfe Verkauf…' : 'Verkauf jetzt prüfen'}
          </button>
        </div>
      </SectionCard>

      <SectionCard
        className="mt-4 border-emerald-900/50"
        title="Backtest (echte Daten)"
        description="Durchsucht die letzten 60 Tage OANDA-Forex-Daten (EUR/USD, GBP/USD) und findet Situationen mit mindestens 4 von 6 erfüllten Bedingungen."
        tooltip="DXY/VIX kommen weiterhin von Yahoo. Weniger oder keine Treffer sind normal — der Backtest dient der Validierung."
      >
        <div className="flex flex-wrap items-end gap-4">
          <div>
            <LabelWithTooltip
              label="Symbol"
              tooltip="OANDA-Instrumente: EUR_USD und GBP_USD."
            />
            <select
              className="bg-gray-900 border border-gray-700 rounded px-3 py-2 mt-1"
              value={backtestSymbol}
              onChange={e => setBacktestSymbol(e.target.value)}
            >
              <option value="EUR_USD">EUR/USD</option>
              <option value="GBP_USD">GBP/USD</option>
            </select>
          </div>
          <button
            className="btn-primary bg-emerald-600 hover:bg-emerald-500"
            title="Lädt 60 Tage H1-Daten, prüft stündlich die Strategie und listet Teil- und Voll-Setups auf. Dauer: ca. 10–30 Sekunden."
            onClick={runBacktest}
            disabled={isBacktesting}
          >
            {isBacktesting ? 'Backtest läuft (10–30s)...' : 'Backtest starten'}
          </button>
        </div>
      </SectionCard>

      {backtestResults.length > 0 && (
        <SectionCard
          className="mt-6 border-emerald-900/40"
          title={`Backtest-Ergebnisse (${backtestListResults.length} starke / ${backtestResults.length} für Chart)`}
          description="Liste: Setups mit 4/6+ Bedingungen. Chart: alle einzelnen Bedingungs-Treffer (auch 1/6) als farbige Quadrate."
          tooltip="Bias = erwartete Richtung (Bullish = Long, Bearish = Short). Konfidenz = Anteil erfüllter Bedingungen (6/6 = 100 %)."
        >
          <div className="mt-2 grid gap-4">
            {backtestListResults.map((res, idx) => (
              <div key={idx} className={`card border ${res.isSetup ? 'border-emerald-500 bg-emerald-900/20' : 'border-gray-700'}`}>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-semibold ${res.bias === 'Bullish' ? 'bg-emerald-700 text-white' : res.bias === 'Bearish' ? 'bg-red-700 text-white' : 'bg-gray-700'}`}>
                      {res.bias}
                    </span>
                    <span className="font-semibold">{res.symbol}</span>
                    {!res.isSetup && <span className="text-xs text-yellow-500 font-medium">Teil-Setup ({Math.round(res.confidence * 100)}%)</span>}
                    {res.isSetup && <span className="text-xs text-emerald-400 font-medium">Perfektes Setup!</span>}
                  </div>
                  <span className="text-xs text-gray-400">
                    {res.detectedAt
                      ? new Date(res.detectedAt).toLocaleString('de-DE', { timeZone: 'Europe/Berlin' })
                      : ''}
                  </span>
                </div>
                
                {res.isSetup && res.opportunity && (
                  <div className="mt-3 grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
                    <Metric label="Einstieg" value={res.opportunity.entryPrice} />
                    <Metric label="Stop-Loss" value={res.opportunity.stopLossPrice} />
                    <Metric label="Take-Profit" value={res.opportunity.takeProfitPrice} />
                    <Metric label="CRV" value={res.opportunity.rewardToRisk} />
                  </div>
                )}

                <details className="mt-3 text-sm" open={res.isSetup}>
                  <summary className="cursor-pointer text-gray-300">Bedingungen ansehen</summary>
                  <ul className="mt-2 space-y-1">
                    {res.conditions.map((c, i) => (
                      <li key={i} className="flex items-start gap-2">
                        <span className={c.passed ? 'text-emerald-400' : 'text-red-500'}>
                          {c.passed ? '✓' : '✗'}
                        </span>
                        <span className={c.passed ? 'text-gray-200' : 'text-gray-400'}>
                          <strong>{c.name}</strong> — {c.detail}
                        </span>
                      </li>
                    ))}
                  </ul>
                </details>
              </div>
            ))}
          </div>
        </SectionCard>
      )}

      {analysis && (
        <SectionCard
          className="mt-4"
          title={`Analyse ${analysis.symbol} (${analysis.exchange})`}
          description={`Bias: ${analysis.bias} — die vom Algorithmus erkannte Marktrichtung für dieses Setup.`}
          tooltip={`Konfidenz ${Math.round(analysis.confidence * 100)} % = ${analysis.conditions.filter(c => c.passed).length} von 6 Bedingungen erfüllt. Grüne Häkchen = bestanden, graue Kreise = noch offen.`}
        >
          <div className="flex items-center justify-end mb-2">
            <span className="text-sm text-gray-400">
              <Tooltip text="Anteil erfüllter Bedingungen. 100 % bedeutet: alle 6 Kriterien sind erfüllt — ein vollständiges Setup.">
                Konfidenz {Math.round(analysis.confidence * 100)}%
              </Tooltip>
            </span>
          </div>
          <ul className="mt-3 space-y-1">
            {analysis.conditions.map((c, i) => (
              <li key={i} className="flex items-start gap-2 text-sm">
                <span className={c.passed ? 'text-emerald-400' : 'text-gray-500'}>
                  {c.passed ? '✓' : '○'}
                </span>
                <span className={c.passed ? 'text-gray-200' : 'text-gray-500'}>
                  <strong>{c.name}</strong> — {c.detail}
                </span>
              </li>
            ))}
          </ul>
        </SectionCard>
      )}

      <SectionCard
        className="mt-6"
        title="Erkannte Trade-Möglichkeiten"
        description="Vom Hintergrund-Scanner gefundene Setups (aktuell SAP Mock, alle 30s). Neue Treffer erscheinen live und aktualisieren diese Liste."
        tooltip="Die Liste wird im RAM gespeichert — nach einem Backend-Neustart ist sie leer, bis der Scanner erneut ein Setup findet."
      >
      <div ref={opportunitiesRef as React.RefObject<HTMLDivElement>} className="setup-opportunities-block">
        <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
          <div className="text-sm text-gray-400">
            {opportunities.length === 0
              ? 'Keine Einträge'
              : hiddenCount > 0
                ? `${visibleOpportunities.length} von ${opportunities.length} angezeigt`
                : `${opportunities.length} ${opportunities.length === 1 ? 'Eintrag' : 'Einträge'}`}
          </div>
          <div className="flex items-center gap-2">
            <LabelWithTooltip
              label="Anzeigen"
              tooltip="Wie viele erkannte Trade-Möglichkeiten in der Liste sichtbar sein sollen. „Alle“ zeigt jeden gespeicherten Treffer."
            />
            <select
              className="bg-gray-900 border border-gray-700 rounded px-3 py-1.5 text-sm"
              value={displayLimit}
              onChange={e => setDisplayLimit(Number(e.target.value))}
            >
              {SETUP_DISPLAY_LIMIT_OPTIONS.map(o => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
            <button
              type="button"
              className="text-xs text-gray-400 hover:text-gray-200 px-2 py-1 rounded border border-gray-700"
              onClick={() => refreshOpportunities()}
              title="Liste vom Server neu laden"
            >
              Aktualisieren
            </button>
          </div>
        </div>

        {newTradeNotice && (
          <div className="demo-success mb-4 flex items-center justify-between gap-3">
            <span>{newTradeNotice}</span>
            <button
              type="button"
              className="text-xs text-emerald-300 hover:text-white"
              onClick={() => setNewTradeNotice(null)}
            >
              Schließen
            </button>
          </div>
        )}

      <div className="grid gap-4">
        {opportunities.length === 0 && (
          <div className="text-sm text-gray-500">
            Noch keine Setups erkannt. Der Scanner läuft im Hintergrund (SAP/XETRA, Mock-Daten).
            Ein grüner Banner erscheint, sobald alle 6 Bedingungen erfüllt sind.
          </div>
        )}
        {visibleOpportunities.map(opp => (
          <div
            key={opp.id}
            role="button"
            tabIndex={0}
            title="Im Chart markieren"
            className={`card cursor-pointer ${highlightId === opp.id ? 'setup-opp--highlight' : ''}`}
            onClick={() => {
              setHighlightId(opp.id)
              setManualLevels({
                entry: opp.entryPrice,
                stopLoss: opp.stopLossPrice,
                takeProfit: opp.takeProfitPrice
              })
            }}
            onKeyDown={e => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault()
                setHighlightId(opp.id)
                setManualLevels({
                  entry: opp.entryPrice,
                  stopLoss: opp.stopLossPrice,
                  takeProfit: opp.takeProfitPrice
                })
              }
            }}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <span
                  className={`px-2 py-0.5 rounded text-xs font-semibold ${
                    opp.direction === 'Long' ? 'bg-emerald-700 text-white' : 'bg-red-700 text-white'
                  }`}
                >
                  {opp.direction}
                </span>
                <span className="font-semibold">{opp.symbol}</span>
                <span className="text-sm text-gray-400">{opp.exchange}</span>
              </div>
              <span className="text-xs text-gray-500">
                {new Date(opp.detectedAt).toLocaleString('de-DE')}
              </span>
            </div>

            <div className="mt-3 grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
              <Metric label="Einstieg" tooltip="Preis in der Mitte des Fair Value Gap — strategischer Einstiegsbereich." value={opp.entryPrice} />
              <Metric label="Stop-Loss" tooltip="Unter dem Liquidity Sweep (Long) — wo das Setup invalidiert wäre." value={opp.stopLossPrice} />
              <Metric label="Take-Profit" tooltip="Ziel bei 2:1 Chance-Risiko-Verhältnis (CRV)." value={opp.takeProfitPrice} />
              <Metric label="CRV" tooltip="Chance-Risiko-Verhältnis: wie viel Gewinn im Verhältnis zum Risiko." value={opp.rewardToRisk} />
            </div>

            <details className="mt-3 text-sm">
              <summary className="cursor-pointer text-gray-300">Bedingungen ({opp.conditions.length}/6)</summary>
              <ul className="mt-2 space-y-1">
                {opp.conditions.map((c, i) => (
                  <li key={i} className="flex items-start gap-2">
                    <span className={c.passed ? 'text-emerald-400' : 'text-gray-500'}>
                      {c.passed ? '✓' : '○'}
                    </span>
                    <span><strong>{c.name}</strong> — {c.detail}</span>
                  </li>
                ))}
              </ul>
            </details>

            <div className="mt-3 flex items-center justify-between">
              <span className="text-xs text-gray-400">Konfidenz {Math.round(opp.confidence * 100)}%</span>
              {opp.status === 'Executed' ? (
                <span className="text-sm text-emerald-400">Ausgeführt</span>
              ) : (
                <button
                  className="btn-primary"
                  title={
                    hasOpenPosition(opp.symbol)
                      ? 'Bereits eine offene Position — erst schließen, dann neu kaufen.'
                      : 'Platziert eine Market-Paper-Order mit Entry, Stop-Loss und Take-Profit.'
                  }
                  disabled={hasOpenPosition(opp.symbol)}
                  onClick={() => execute(opp)}
                >
                  {hasOpenPosition(opp.symbol) ? 'Bereits investiert' : 'Trade ausführen'}
                </button>
              )}
            </div>
          </div>
        ))}
        {hiddenCount > 0 && (
          <p className="text-xs text-gray-500 text-center py-2">
            {hiddenCount} weitere {hiddenCount === 1 ? 'Möglichkeit' : 'Möglichkeiten'} ausgeblendet —
            wähle „Alle“ oder eine höhere Anzahl unter „Anzeigen“.
          </p>
        )}
      </div>
      </div>
      </SectionCard>
    </div>
  )
}

function Metric({ label, value, tooltip }: { label: string; value: number; tooltip?: string }) {
  return (
    <div className="bg-gray-900 border border-gray-800 rounded px-3 py-2">
      <div className="text-xs text-gray-500">
        {tooltip ? <Tooltip text={tooltip}>{label}</Tooltip> : label}
      </div>
      <div className="font-mono">{value}</div>
    </div>
  )
}
