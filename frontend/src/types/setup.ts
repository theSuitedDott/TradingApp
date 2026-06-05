import { field } from '../utils/format'

/** Single condition check from the institutional setup strategy. */
export type SetupCondition = {
  name: string
  passed: boolean
  detail: string
}

/** Trade opportunity emitted when setup conditions are met. */
export type SetupOpportunity = {
  id: string
  symbol: string
  exchange: string
  direction: string
  side: string
  entryPrice: number
  stopLossPrice: number
  takeProfitPrice: number
  rewardToRisk: number
  confidence: number
  message: string
  detectedAt: string
  status: string
  conditions: SetupCondition[]
}

/** Normalizes API / SignalR payloads (camelCase or PascalCase). */
export function normalizeOpportunity(raw: Record<string, unknown>): SetupOpportunity {
  const conditionsRaw = field<Record<string, unknown>[]>(raw, 'conditions', 'Conditions') ?? []
  return {
    id: String(field(raw, 'id', 'Id') ?? ''),
    symbol: String(field(raw, 'symbol', 'Symbol') ?? ''),
    exchange: String(field(raw, 'exchange', 'Exchange') ?? ''),
    direction: String(field(raw, 'direction', 'Direction') ?? ''),
    side: String(field(raw, 'side', 'Side') ?? ''),
    entryPrice: Number(field(raw, 'entryPrice', 'EntryPrice') ?? 0),
    stopLossPrice: Number(field(raw, 'stopLossPrice', 'StopLossPrice') ?? 0),
    takeProfitPrice: Number(field(raw, 'takeProfitPrice', 'TakeProfitPrice') ?? 0),
    rewardToRisk: Number(field(raw, 'rewardToRisk', 'RewardToRisk') ?? 0),
    confidence: Number(field(raw, 'confidence', 'Confidence') ?? 0),
    message: String(field(raw, 'message', 'Message') ?? ''),
    detectedAt: String(field(raw, 'detectedAt', 'DetectedAt') ?? new Date().toISOString()),
    status: String(field(raw, 'status', 'Status') ?? 'Active'),
    conditions: conditionsRaw.map(c => ({
      name: String(field(c, 'name', 'Name') ?? ''),
      passed: Boolean(field(c, 'passed', 'Passed')),
      detail: String(field(c, 'detail', 'Detail') ?? '')
    }))
  }
}

/** Returns true when all six institutional setup conditions are satisfied. */
export function isFullSetup(opportunity: SetupOpportunity): boolean {
  if (opportunity.conditions.length >= 6) {
    return opportunity.conditions.every(c => c.passed)
  }
  return opportunity.confidence >= 0.999
}

/** Count of passed conditions (max 6). */
export function passedConditionCount(opportunity: SetupOpportunity): number {
  if (opportunity.conditions.length === 0) {
    return Math.round(opportunity.confidence * 6)
  }
  return opportunity.conditions.filter(c => c.passed).length
}
