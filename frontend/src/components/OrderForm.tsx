import React, { useState } from 'react'
import { OrderSide, OrderType } from '../types'

type Props = {
  onPlace: (payload: Record<string, unknown>) => Promise<void>
  disabled?: boolean
}

export default function OrderForm({ onPlace, disabled = false }: Props) {
  const [symbol, setSymbol] = useState('SAP')
  const [exchange, setExchange] = useState('XETRA')
  const [side, setSide] = useState<OrderSide>(OrderSide.Buy)
  const [type, setType] = useState<OrderType>(OrderType.Market)
  const [quantity, setQuantity] = useState(1)
  const [limitPrice, setLimitPrice] = useState<number | undefined>(undefined)
  const [submitting, setSubmitting] = useState(false)

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    if (disabled) return
    const payload: Record<string, unknown> = { symbol, exchange, side, type, quantity }
    if (type === OrderType.Limit) payload.limitPrice = limitPrice
    setSubmitting(true)
    try {
      await onPlace(payload)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="order-form" onSubmit={submit}>
      <div className="order-form-grid">
        <div className="order-field">
          <label>Symbol</label>
          <input value={symbol} onChange={e => setSymbol(e.target.value)} disabled={disabled} />
        </div>
        <div className="order-field">
          <label>Börse</label>
          <input value={exchange} onChange={e => setExchange(e.target.value)} disabled={disabled} />
        </div>
        <div className="order-field">
          <label>Richtung</label>
          <select value={side} onChange={e => setSide(Number(e.target.value))} disabled={disabled}>
            <option value={OrderSide.Buy}>Kaufen</option>
            <option value={OrderSide.Sell}>Verkaufen</option>
          </select>
        </div>
        <div className="order-field">
          <label>Ordertyp</label>
          <select value={type} onChange={e => setType(Number(e.target.value))} disabled={disabled}>
            <option value={OrderType.Market}>Market</option>
            <option value={OrderType.Limit}>Limit</option>
          </select>
        </div>
        <div className="order-field">
          <label>Menge</label>
          <input
            type="number"
            min={1}
            step={1}
            value={quantity}
            onChange={e => setQuantity(Math.max(1, parseInt(e.target.value, 10) || 1))}
            disabled={disabled}
          />
        </div>
        {type === OrderType.Limit && (
          <div className="order-field">
            <label>Limit-Preis</label>
            <input
              type="number"
              step="0.01"
              value={limitPrice ?? ''}
              onChange={e => setLimitPrice(Number(e.target.value))}
              disabled={disabled}
            />
          </div>
        )}
      </div>
      <button type="submit" className="btn-primary w-full mt-3" disabled={disabled || submitting}>
        {submitting ? 'Order wird platziert…' : 'Order platzieren'}
      </button>
    </form>
  )
}
