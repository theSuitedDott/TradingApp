import React, { useEffect, useState } from 'react'
import { accounts, orders } from '../api'

export default function Paper() {
  const [accountsList, setAccountsList] = useState<any[]>([])
  const [ordersList, setOrdersList] = useState<any[]>([])

  useEffect(() => { load() }, [])

  async function load() {
    try {
      const a = await accounts.list()
      setAccountsList(a)
      if (a.length > 0) {
        const o = await orders.list(a[0].id)
        setOrdersList(o)
      }
    } catch (ex) { console.error(ex) }
  }

  return (
    <div>
      <h2 className="text-2xl font-semibold">Paper Trading</h2>
      <div className="mt-4 grid md:grid-cols-2 gap-4">
        <div className="card">
          <h3 className="font-medium">Accounts</h3>
          {accountsList.map(a => <div key={a.id} className="py-2 border-b border-gray-800">{a.name} — {a.baseCurrency}</div>)}
        </div>
        <div className="card">
          <h3 className="font-medium">Recent Orders</h3>
          {ordersList.map(o => <div key={o.id} className="py-2 border-b border-gray-800">{o.symbol} {o.status}</div>)}
        </div>
      </div>
    </div>
  )
}

