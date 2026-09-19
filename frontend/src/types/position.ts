import { field } from '../utils/format'

/** Open paper position used for sell alert monitoring. */
export type OpenPosition = {
  id: string
  symbol: string
  exchange: string
  side: string
  quantity: number
  stopLossPrice?: number
  takeProfitPrice?: number
}

/** Normalizes position API payloads (camelCase or PascalCase). */
export function normalizeOpenPosition(raw: Record<string, unknown>): OpenPosition {
  const stop = field(raw, 'stopLossPrice', 'StopLossPrice')
  const take = field(raw, 'takeProfitPrice', 'TakeProfitPrice')
  return {
    id: String(field(raw, 'id', 'Id') ?? ''),
    symbol: String(field(raw, 'symbol', 'Symbol') ?? ''),
    exchange: String(field(raw, 'exchange', 'Exchange') ?? ''),
    side: String(field(raw, 'side', 'Side') ?? ''),
    quantity: Number(field(raw, 'quantity', 'Quantity') ?? 0),
    stopLossPrice: stop != null ? Number(stop) : undefined,
    takeProfitPrice: take != null ? Number(take) : undefined
  }
}

/** Returns true when the symbol already has an open long/short position. */
export function hasPositionForSymbol(positions: OpenPosition[], symbol: string): boolean {
  const key = symbol.trim().toUpperCase()
  return positions.some(p => p.symbol.trim().toUpperCase() === key)
}
