/** Marker placement to avoid overlap when several conditions hit the same candle. */
export type MarkerPlacement = 'aboveBar' | 'belowBar' | 'inBar'

/** Toggleable chart layer for one institutional setup condition. */
export type SetupConditionLayer = {
  id: string
  label: string
  shortLabel: string
  color: string
  namePattern: RegExp
  position: MarkerPlacement
  /** Entry at 3-push; all other conditions render as exit markers. */
  role: 'entry' | 'exit'
}

/** Chart layer id for 3-push entry markers. */
export const ENTRY_3PUSH_LAYER_ID = 'entry-3push'

/** Prefix for exit markers derived from setup conditions. */
export const EXIT_LAYER_PREFIX = 'exit-'

/** The six setup conditions — used for chart markers and checkbox filters. */
export const SETUP_CONDITION_LAYERS: SetupConditionLayer[] = [
  { id: 'cond-trend', label: 'Klarer H4-Trend', shortLabel: 'H4', color: '#60a5fa', namePattern: /H4-Trend/i, position: 'belowBar', role: 'exit' },
  { id: 'cond-exhaustion', label: '3-Push Entry', shortLabel: 'ENTRY', color: '#10b981', namePattern: /3-Push|3 Push|Erschöpfung/i, position: 'belowBar', role: 'entry' },
  { id: 'cond-sweep', label: 'Liquiditäts-Sweep', shortLabel: 'Sweep', color: '#f472b6', namePattern: /Liquidität|Sweep|Inducement/i, position: 'aboveBar', role: 'exit' },
  { id: 'cond-displacement', label: 'Displacement', shortLabel: 'Disp', color: '#fb923c', namePattern: /Displacement/i, position: 'aboveBar', role: 'exit' },
  { id: 'cond-fvg', label: 'Fair Value Gap', shortLabel: 'FVG', color: '#34d399', namePattern: /Fair Value|FVG/i, position: 'aboveBar', role: 'exit' },
  { id: 'cond-macro', label: 'DXY/VIX', shortLabel: 'DXY', color: '#facc15', namePattern: /DXY|VIX/i, position: 'aboveBar', role: 'exit' }
]

/** Non-condition chart layers (signals, exits). */
export const CHART_SIGNAL_LAYERS = [
  { id: 'backtest-full', label: 'Backtest Voll-Setup (6/6)', color: '#10b981' },
  { id: 'backtest-partial', label: 'Backtest Teil-Setup', color: '#38bdf8' },
  { id: 'live-full', label: 'Live Kauf-Signal', color: '#22c55e' },
  { id: 'live-partial', label: 'Live Teilsignal', color: '#f59e0b' },
  { id: 'exit', label: 'Verkaufs-Alerts (SL/TP)', color: '#ef4444' }
] as const

/** Default visibility for all chart marker layers. */
export function createDefaultLayerVisibility(): Record<string, boolean> {
  const layers: Record<string, boolean> = { [ENTRY_3PUSH_LAYER_ID]: true }
  for (const layer of SETUP_CONDITION_LAYERS) {
    if (layer.role === 'exit') {
      layers[EXIT_LAYER_PREFIX + layer.id] = true
    }
  }
  for (const layer of CHART_SIGNAL_LAYERS) {
    layers[layer.id] = layer.id === 'exit' || layer.id === 'live-full'
  }
  return layers
}

/** Maps a condition display name from the API to a stable layer id. */
export function resolveConditionLayerId(name: string): string | null {
  return SETUP_CONDITION_LAYERS.find(layer => layer.namePattern.test(name))?.id ?? null
}

/** Chart layer id used for a passed condition (entry vs exit). */
export function chartLayerForCondition(conditionLayerId: string): string {
  const def = SETUP_CONDITION_LAYERS.find(l => l.id === conditionLayerId)
  if (def?.role === 'entry') return ENTRY_3PUSH_LAYER_ID
  return EXIT_LAYER_PREFIX + conditionLayerId
}
