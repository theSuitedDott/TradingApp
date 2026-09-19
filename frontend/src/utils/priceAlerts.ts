/** Returns true when price crossed a level between previous and next tick. */
export function crossedLevel(prev: number, next: number, level: number): boolean {
  return (prev < level && next >= level) || (prev > level && next <= level)
}

/** Long position: stop-loss triggered when price falls to or below level. */
export function hitStopLoss(prev: number, price: number, stopLoss: number): boolean {
  return prev > stopLoss && price <= stopLoss
}

/** Long position: take-profit triggered when price rises to or above level. */
export function hitTakeProfit(prev: number, price: number, takeProfit: number): boolean {
  return prev < takeProfit && price >= takeProfit
}
