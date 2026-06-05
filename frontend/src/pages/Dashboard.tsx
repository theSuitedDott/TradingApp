import React, { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { accounts, orders } from '../api'
import DemoAccountSetup from '../components/DemoAccountSetup'
import OrderForm from '../components/OrderForm'
import SectionCard from '../components/SectionCard'
import { useAuth } from '../context/AuthContext'
import { field, formatDate, formatDateTime, formatMoney, userDisplayName, userInitials } from '../utils/format'

type PaperAccount = {
  id: string
  name: string
  baseCurrency: string
  initialBalance: number
  isDefault: boolean
  status: string
  createdAt: string
}

type Portfolio = {
  cashBalance: number
  reservedCash: number
  availableCash: number
  totalEquity: number
  unrealizedPnL: number
  realizedPnL: number
  baseCurrency: string
}

type Position = {
  id: string
  symbol: string
  exchange: string
  side: string
  quantity: number
  averageEntryPrice: number
  currentPrice?: number | null
  unrealizedPnL: number
}

type Order = {
  id: string
  symbol: string
  exchange: string
  side: string
  type: string
  status: string
  quantity: number
  createdAt: string
}

function normalizeAccount(raw: Record<string, unknown>): PaperAccount {
  return {
    id: String(field(raw, 'id', 'Id') ?? ''),
    name: String(field(raw, 'name', 'Name') ?? 'Konto'),
    baseCurrency: String(field(raw, 'baseCurrency', 'BaseCurrency') ?? 'EUR'),
    initialBalance: Number(field(raw, 'initialBalance', 'InitialBalance') ?? 0),
    isDefault: Boolean(field(raw, 'isDefault', 'IsDefault')),
    status: String(field(raw, 'status', 'Status') ?? 'Active'),
    createdAt: String(field(raw, 'createdAt', 'CreatedAt') ?? '')
  }
}

function normalizePortfolio(raw: Record<string, unknown>): Portfolio {
  return {
    cashBalance: Number(field(raw, 'cashBalance', 'CashBalance') ?? 0),
    reservedCash: Number(field(raw, 'reservedCash', 'ReservedCash') ?? 0),
    availableCash: Number(field(raw, 'availableCash', 'AvailableCash') ?? 0),
    totalEquity: Number(field(raw, 'totalEquity', 'TotalEquity') ?? 0),
    unrealizedPnL: Number(field(raw, 'unrealizedPnL', 'UnrealizedPnL') ?? 0),
    realizedPnL: Number(field(raw, 'realizedPnL', 'RealizedPnL') ?? 0),
    baseCurrency: String(field(raw, 'baseCurrency', 'BaseCurrency') ?? 'EUR')
  }
}

function normalizePosition(raw: Record<string, unknown>): Position {
  return {
    id: String(field(raw, 'id', 'Id') ?? ''),
    symbol: String(field(raw, 'symbol', 'Symbol') ?? ''),
    exchange: String(field(raw, 'exchange', 'Exchange') ?? ''),
    side: String(field(raw, 'side', 'Side') ?? ''),
    quantity: Number(field(raw, 'quantity', 'Quantity') ?? 0),
    averageEntryPrice: Number(field(raw, 'averageEntryPrice', 'AverageEntryPrice') ?? 0),
    currentPrice: field<number | null>(raw, 'currentPrice', 'CurrentPrice'),
    unrealizedPnL: Number(field(raw, 'unrealizedPnL', 'UnrealizedPnL') ?? 0)
  }
}

function normalizeOrder(raw: Record<string, unknown>): Order {
  return {
    id: String(field(raw, 'id', 'Id') ?? ''),
    symbol: String(field(raw, 'symbol', 'Symbol') ?? ''),
    exchange: String(field(raw, 'exchange', 'Exchange') ?? ''),
    side: String(field(raw, 'side', 'Side') ?? ''),
    type: String(field(raw, 'type', 'Type') ?? ''),
    status: String(field(raw, 'status', 'Status') ?? ''),
    quantity: Number(field(raw, 'quantity', 'Quantity') ?? 0),
    createdAt: String(field(raw, 'createdAt', 'CreatedAt') ?? '')
  }
}

function pnlClass(value: number): string {
  if (value > 0) return 'dash-pnl--pos'
  if (value < 0) return 'dash-pnl--neg'
  return 'dash-pnl--zero'
}

export default function Dashboard() {
  const { user } = useAuth()
  const [accountsList, setAccountsList] = useState<PaperAccount[]>([])
  const [selected, setSelected] = useState<string | null>(null)
  const [portfolio, setPortfolio] = useState<Portfolio | null>(null)
  const [positions, setPositions] = useState<Position[]>([])
  const [ordersList, setOrdersList] = useState<Order[]>([])
  const [loading, setLoading] = useState(true)

  const selectedAccount = accountsList.find(a => a.id === selected) ?? null

  const refreshAccountData = useCallback(async (accountId: string) => {
    const [portRaw, posRaw, ordRaw] = await Promise.all([
      accounts.portfolio(accountId),
      accounts.positions(accountId),
      orders.list(accountId)
    ])
    setPortfolio(normalizePortfolio(portRaw ?? {}))
    setPositions((posRaw ?? []).map((p: Record<string, unknown>) => normalizePosition(p)))
    setOrdersList((ordRaw ?? []).map((o: Record<string, unknown>) => normalizeOrder(o)).slice(0, 6))
  }, [])

  const loadAccounts = useCallback(async () => {
    setLoading(true)
    try {
      const res = await accounts.list()
      const list = (res ?? []).map((a: Record<string, unknown>) => normalizeAccount(a))
      setAccountsList(list)
      const defaultAccount = list.find(a => a.isDefault) ?? list[0]
      if (defaultAccount) {
        setSelected(defaultAccount.id)
        await refreshAccountData(defaultAccount.id)
      } else {
        setSelected(null)
        setPortfolio(null)
        setPositions([])
        setOrdersList([])
      }
    } catch {
      setAccountsList([])
    } finally {
      setLoading(false)
    }
  }, [refreshAccountData])

  useEffect(() => {
    loadAccounts()
  }, [loadAccounts])

  useEffect(() => {
    if (!selected) return
    refreshAccountData(selected).catch(() => undefined)
  }, [selected, refreshAccountData])

  async function placeOrder(payload: Record<string, unknown>) {
    if (!selected) return
    await orders.place(selected, payload)
    await refreshAccountData(selected)
  }

  const currency = portfolio?.baseCurrency ?? selectedAccount?.baseCurrency ?? 'EUR'
  const greeting = user ? userDisplayName(user) : 'Trader'

  return (
    <div className="dashboard-page">
      <header className="dash-hero">
        <div>
          <p className="dash-hero-kicker">Portfolio-Übersicht</p>
          <h2 className="dash-hero-title">Willkommen zurück, {greeting}</h2>
          <p className="dash-hero-sub">
            Dein Dashboard zeigt Kontostand, offene Positionen und letzte Orders auf einen Blick.
          </p>
        </div>
        <div className="dash-hero-actions">
          <Link to="/setups" className="dash-action-btn dash-action-btn--primary">Zu Setups</Link>
          <Link to="/paper" className="dash-action-btn">Paper Trading</Link>
        </div>
      </header>

      <div className="dash-profile-grid">
        {user && (
          <SectionCard title="Dein Konto" description="Angemeldeter Benutzer und Rolle.">
            <div className="dash-user-panel">
              <div className="dash-user-avatar-lg" aria-hidden="true">
                {userInitials(user)}
              </div>
              <dl className="dash-meta-list">
                <div>
                  <dt>Name</dt>
                  <dd>{user.firstName || user.lastName ? `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim() : '—'}</dd>
                </div>
                <div>
                  <dt>E-Mail</dt>
                  <dd>{user.email}</dd>
                </div>
                <div>
                  <dt>Rolle</dt>
                  <dd><span className="dash-badge">{user.role}</span></dd>
                </div>
                <div>
                  <dt>Mitglied seit</dt>
                  <dd>{formatDate(user.createdAt)}</dd>
                </div>
              </dl>
            </div>
          </SectionCard>
        )}

        <SectionCard
          title="Paper-Konten"
          description="Wähle das virtuelle Handelskonto für Portfolio und Orders."
        >
          {loading && <p className="text-sm text-gray-500">Lade Konten…</p>}
          {!loading && accountsList.length === 0 && (
            <DemoAccountSetup compact onChanged={loadAccounts} />
          )}
          <div className="dash-account-list">
            {accountsList.map(a => (
              <button
                key={a.id}
                type="button"
                className={`dash-account-card ${selected === a.id ? 'dash-account-card--active' : ''}`}
                onClick={() => setSelected(a.id)}
              >
                <div className="dash-account-card-head">
                  <strong>{a.name}</strong>
                  {a.isDefault && <span className="dash-badge dash-badge--muted">Standard</span>}
                </div>
                <div className="dash-account-card-meta">
                  <span>{formatMoney(a.initialBalance, a.baseCurrency)} Startkapital</span>
                  <span>{a.status} · {formatDate(a.createdAt)}</span>
                </div>
              </button>
            ))}
          </div>
        </SectionCard>
      </div>

      {selectedAccount && portfolio && (
        <>
          <div className="dash-kpi-grid">
            <div className="dash-kpi">
              <span className="dash-kpi-label">Gesamtvermögen</span>
              <span className="dash-kpi-value">{formatMoney(portfolio.totalEquity, currency)}</span>
              <span className="dash-kpi-hint">Cash + offene Positionen</span>
            </div>
            <div className="dash-kpi">
              <span className="dash-kpi-label">Verfügbar</span>
              <span className="dash-kpi-value">{formatMoney(portfolio.availableCash, currency)}</span>
              <span className="dash-kpi-hint">Reserviert: {formatMoney(portfolio.reservedCash, currency)}</span>
            </div>
            <div className="dash-kpi">
              <span className="dash-kpi-label">Unrealisierter PnL</span>
              <span className={`dash-kpi-value ${pnlClass(portfolio.unrealizedPnL)}`}>
                {formatMoney(portfolio.unrealizedPnL, currency)}
              </span>
              <span className="dash-kpi-hint">Offene Positionen</span>
            </div>
            <div className="dash-kpi">
              <span className="dash-kpi-label">Realisierter PnL</span>
              <span className={`dash-kpi-value ${pnlClass(portfolio.realizedPnL)}`}>
                {formatMoney(portfolio.realizedPnL, currency)}
              </span>
              <span className="dash-kpi-hint">Geschlossene Trades</span>
            </div>
          </div>

          <div className="dash-content-grid">
            <SectionCard title="Offene Positionen" description={`Konto: ${selectedAccount.name}`}>
              {positions.length === 0 ? (
                <p className="text-sm text-gray-500">Keine offenen Positionen.</p>
              ) : (
                <div className="dash-table-wrap">
                  <table className="dash-table">
                    <thead>
                      <tr>
                        <th>Symbol</th>
                        <th>Seite</th>
                        <th>Menge</th>
                        <th>Einstieg</th>
                        <th>Aktuell</th>
                        <th>PnL</th>
                      </tr>
                    </thead>
                    <tbody>
                      {positions.map(p => (
                        <tr key={p.id}>
                          <td>
                            <strong>{p.symbol}</strong>
                            <span className="dash-table-sub">{p.exchange}</span>
                          </td>
                          <td><span className={`dash-side dash-side--${p.side.toLowerCase()}`}>{p.side}</span></td>
                          <td>{p.quantity}</td>
                          <td>{formatMoney(p.averageEntryPrice, currency)}</td>
                          <td>{p.currentPrice != null ? formatMoney(p.currentPrice, currency) : '—'}</td>
                          <td className={pnlClass(p.unrealizedPnL)}>{formatMoney(p.unrealizedPnL, currency)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </SectionCard>

            <div className="dash-side-stack">
              <SectionCard title="Letzte Orders" description="Die 6 neuesten Orders dieses Kontos.">
                {ordersList.length === 0 ? (
                  <p className="text-sm text-gray-500">Noch keine Orders platziert.</p>
                ) : (
                  <ul className="dash-order-list">
                    {ordersList.map(o => (
                      <li key={o.id} className="dash-order-item">
                        <div>
                          <strong>{o.symbol}</strong>
                          <span className="dash-table-sub">{o.exchange} · {o.type}</span>
                        </div>
                        <div className="dash-order-item-right">
                          <span className={`dash-side dash-side--${o.side.toLowerCase()}`}>{o.side}</span>
                          <span className="dash-order-status">{o.status}</span>
                          <span className="dash-table-sub">{formatDateTime(o.createdAt)}</span>
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
              </SectionCard>

              <SectionCard title="Schnell-Order" description="Manuelle Paper-Order auf dem gewählten Konto.">
                <OrderForm onPlace={placeOrder} disabled={!selected} />
              </SectionCard>
            </div>
          </div>
        </>
      )}

      <SectionCard
        className="mt-2"
        title="Setup-Analyse"
        description="Signal-Erkennung und Trades laufen bewusst im Setups-Reiter — getrennt von der Portfolio-Übersicht."
      >
        <p className="text-sm text-gray-400">
          Strategie prüfen, Chart ansehen, Backtest starten und Ein-Klick-Trades ausführen:{' '}
          <Link to="/setups" className="text-blue-400 underline">Zum Setups-Arbeitsplatz</Link>
        </p>
      </SectionCard>
    </div>
  )
}
