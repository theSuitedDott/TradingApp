import React, { useState } from 'react'
import { OrderSide, OrderType } from '../types'

export default function OrderForm({ onPlace }: { onPlace: (payload:any)=>Promise<void> }) {
  const [symbol, setSymbol] = useState('SAP')
  const [exchange, setExchange] = useState('XETRA')
  const [side, setSide] = useState<OrderSide>(OrderSide.Buy)
  const [type, setType] = useState<OrderType>(OrderType.Market)
  const [quantity, setQuantity] = useState(1)
  const [limitPrice, setLimitPrice] = useState<number|undefined>(undefined)

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    const payload: any = {
      symbol, exchange, side, type, quantity
    }
    if (type === OrderType.Limit) payload.limitPrice = limitPrice
    await onPlace(payload)
  }

  return (
    <form className="orderForm" onSubmit={submit}>
      <h3>Place Order</h3>
      <label>Symbol</label><input value={symbol} onChange={e=>setSymbol(e.target.value)} />
      <label>Exchange</label><input value={exchange} onChange={e=>setExchange(e.target.value)} />
      <label>Side</label>
      <select value={side} onChange={e=>setSide(Number(e.target.value))}>
        <option value={OrderSide.Buy}>Buy</option>
        <option value={OrderSide.Sell}>Sell</option>
      </select>
      <label>Type</label>
      <select value={type} onChange={e=>setType(Number(e.target.value))}>
        <option value={OrderType.Market}>Market</option>
        <option value={OrderType.Limit}>Limit</option>
      </select>
      <label>Quantity</label><input type="number" value={quantity} onChange={e=>setQuantity(Number(e.target.value))} />
      {type === OrderType.Limit && <>
        <label>Limit Price</label><input type="number" step="0.01" onChange={e=>setLimitPrice(Number(e.target.value))} />
      </>}
      <button type="submit">Place</button>
    </form>
  )
}

