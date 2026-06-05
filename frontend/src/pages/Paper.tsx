import React, { useCallback, useEffect, useState } from 'react'
import { accounts, orders } from '../api'
import DemoAccountSetup from '../components/DemoAccountSetup'
import SectionCard from '../components/SectionCard'
import { field, formatDateTime, formatMoney } from '../utils/format'

type PaperAccount = {
  id: string
  name: string
  baseCurrency: string
  initialBalance: number
  isDefault: boolean
  status: string
}

function normalizeAccount(raw: Record<string, unknown>): PaperAccount {
  return {
    id: String(field(raw, 'id', 'Id') ?? ''),
    name: String(field(raw, 'name', 'Name') ?? ''),
    baseCurrency: String(field(raw, 'baseCurrency', 'BaseCurrency') ?? 'EUR'),
    initialBalance: Number(field(raw, 'initialBalance', 'InitialBalance') ?? 0),
    isDefault: Boolean(field(raw, 'isDefault', 'IsDefault')),
    status: String(field(raw, 'status', 'Status') ?? 'Active')
  }
}

export default function Paper() {
  const [accountsList, setAccountsList] = useState<PaperAccount[]>([])
  const [ordersList, setOrdersList] = useState<any[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const a = await accounts.list()
      const list = (a ?? []).map((x: Record<string, unknown>) => normalizeAccount(x))
      setAccountsList(list)
      const active = list.find(x => x.id === selectedId) ?? list.find(x => x.isDefault) ?? list[0]
      if (active) {
        setSelectedId(active.id)
        const o = await orders.list(active.id)
        setOrdersList(o ?? [])
      } else {
        setSelectedId(null)
        setOrdersList([])
      }
    } catch {
      setAccountsList([])
      setOrdersList([])
    } finally {
      setLoading(false)
    }
  }, [selectedId])

  useEffect(() => {
    load()
  }, [])

  return (
    <div className="dashboard-page">
      <header className="dash-hero">
        <div>
          <p className="dash-hero-kicker">Paper Trading</p>
          <h2 className="dash-hero-title">Demo-Börsenkonto</h2>
          <p className="dash-hero-sub">
            Lege ein virtuelles Depot an oder binde einen Demo-Broker an — ohne echtes Geld.
          </p>
        </div>
      </header>

      <DemoAccountSetup onChanged={load} />

      {!loading && accountsList.length > 0 && (
        <div className="grid md:grid-cols-2 gap-4 mt-6">
          <SectionCard title="Deine Paper-Konten">
            <div className="dash-account-list">
              {accountsList.map(a => (
                <button
                  key={a.id}
                  type="button"
                  className={`dash-account-card ${selectedId === a.id ? 'dash-account-card--active' : ''}`}
                  onClick={async () => {
                    setSelectedId(a.id)
                    const o = await orders.list(a.id)
                    setOrdersList(o ?? [])
                  }}
                >
                  <div className="dash-account-card-head">
                    <strong>{a.name}</strong>
                    {a.isDefault && <span className="dash-badge dash-badge--muted">Standard</span>}
                  </div>
                  <div className="dash-account-card-meta">
                    <span>{formatMoney(a.initialBalance, a.baseCurrency)} Startkapital</span>
                    <span>{a.status}</span>
                  </div>
                </button>
              ))}
            </div>
          </SectionCard>

          <SectionCard title="Letzte Orders">
            {ordersList.length === 0 ? (
              <p className="text-sm text-gray-500">Noch keine Orders auf diesem Konto.</p>
            ) : (
              <ul className="dash-order-list">
                {ordersList.slice(0, 8).map((o: Record<string, unknown>) => (
                  <li key={String(field(o, 'id', 'Id'))} className="dash-order-item">
                    <div>
                      <strong>{String(field(o, 'symbol', 'Symbol'))}</strong>
                      <span className="dash-table-sub">{String(field(o, 'status', 'Status'))}</span>
                    </div>
                    <span className="text-xs text-gray-500">
                      {formatDateTime(String(field(o, 'createdAt', 'CreatedAt') ?? ''))}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </SectionCard>
        </div>
      )}
    </div>
  )
}
