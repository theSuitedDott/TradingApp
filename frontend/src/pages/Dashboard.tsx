import React, { useEffect, useState } from 'react'
import { accounts, orders, market } from '../api'

import OrderForm from '../components/OrderForm'

export default function Dashboard() {
  const [accountsList, setAccountsList] = useState<any[]>([])
  const [selected, setSelected] = useState<string | null>(null)
  const [positions, setPositions] = useState<any[]>([])

  useEffect(() => {
    load()
  }, [])

  async function load() {
    try {
      const res = await accounts.list()
      setAccountsList(res)
      if (res.length > 0) setSelected(res[0].id)
    } catch { /* ignore */ }
  }

  async function placeOrder(payload: any) {
    if (!selected) return
    await orders.place(selected, payload)
    await refreshOrders()
  }

  async function refreshOrders() {
    if (!selected) return
    const ord = await orders.list(selected)
    // not displayed now
    await refreshPortfolio()
  }

  async function refreshPortfolio() {
    if (!selected) return
    const p = await accounts.portfolio(selected)
    setPositions(p ? p.positions ?? [] : [])
  }

  async function submitQuote() {
    await market.submitQuote({ symbol: 'SAP', exchange: 'XETRA', price: Math.round(Math.random()*200) })
  }

  return (
    <div className="dashboard">
      <aside className="sidebar">
        <h3>Accounts</h3>
        {accountsList.map(a => (
          <div key={a.id} className={`account ${selected === a.id ? 'active' : ''}`} onClick={() => setSelected(a.id)}>
            <strong>{a.name}</strong>
            <div>{a.baseCurrency} {a.initialBalance}</div>
          </div>
        ))}
        <button onClick={submitQuote}>Submit Random Quote (demo)</button>
      </aside>
      <section className="content">
        <h2>Account: {selected}</h2>
        <OrderForm onPlace={placeOrder} />
        <h3>Positions</h3>
        <table>
          <thead><tr><th>Symbol</th><th>Qty</th><th>Avg</th><th>Unreal</th></tr></thead>
          <tbody>
            {positions.map((p:any) => (
              <tr key={p.id}><td>{p.symbol}</td><td>{p.quantity}</td><td>{p.averageEntryPrice}</td><td>{p.unrealizedPnL}</td></tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  )
}

