import React, { useEffect, useState } from 'react'
import { demo } from '../api'
import { useAuth } from '../context/AuthContext'
import { field, formatMoney } from '../utils/format'
import SectionCard from './SectionCard'

type Preset = {
  id: string
  name: string
  description: string
  initialBalance: number
  baseCurrency: string
  accountType: string
}

type BrokerLink = {
  id: string
  name: string
  brokerName: string
  externalAccountId?: string | null
  connectionMode: string
  isLiveTradingEnabled: boolean
}

type SetupStatus = {
  hasPaperAccount: boolean
  paperAccountCount: number
  linkedBrokerCount: number
  recommendedAction: string
  linkedBrokers: BrokerLink[]
}

type Props = {
  onChanged?: () => void
  compact?: boolean
}

function normalizePreset(raw: Record<string, unknown>): Preset {
  return {
    id: String(field(raw, 'id', 'Id') ?? ''),
    name: String(field(raw, 'name', 'Name') ?? ''),
    description: String(field(raw, 'description', 'Description') ?? ''),
    initialBalance: Number(field(raw, 'initialBalance', 'InitialBalance') ?? 0),
    baseCurrency: String(field(raw, 'baseCurrency', 'BaseCurrency') ?? 'EUR'),
    accountType: String(field(raw, 'accountType', 'AccountType') ?? 'Paper')
  }
}

function normalizeBroker(raw: Record<string, unknown>): BrokerLink {
  return {
    id: String(field(raw, 'id', 'Id') ?? ''),
    name: String(field(raw, 'name', 'Name') ?? ''),
    brokerName: String(field(raw, 'brokerName', 'BrokerName') ?? ''),
    externalAccountId: field<string | null>(raw, 'externalAccountId', 'ExternalAccountId'),
    connectionMode: String(field(raw, 'connectionMode', 'ConnectionMode') ?? 'Demo'),
    isLiveTradingEnabled: Boolean(field(raw, 'isLiveTradingEnabled', 'IsLiveTradingEnabled'))
  }
}

function normalizeStatus(raw: Record<string, unknown>): SetupStatus {
  const brokers = (field<Record<string, unknown>[]>(raw, 'linkedBrokers', 'LinkedBrokers') ?? [])
    .map(normalizeBroker)
  return {
    hasPaperAccount: Boolean(field(raw, 'hasPaperAccount', 'HasPaperAccount')),
    paperAccountCount: Number(field(raw, 'paperAccountCount', 'PaperAccountCount') ?? 0),
    linkedBrokerCount: Number(field(raw, 'linkedBrokerCount', 'LinkedBrokerCount') ?? 0),
    recommendedAction: String(field(raw, 'recommendedAction', 'RecommendedAction') ?? ''),
    linkedBrokers: brokers
  }
}

function parseError(message: string): string {
  if (message.includes('409')) return 'Ein Konto oder eine Verbindung mit diesem Namen existiert bereits.'
  if (message.includes('400')) return 'Bitte prüfe deine Eingaben.'
  if (message.startsWith('HTTP')) return 'Server nicht erreichbar. Läuft das Backend?'
  return message
}

/** Wizard for creating demo paper accounts and linking demo brokers. */
export default function DemoAccountSetup({ onChanged, compact = false }: Props) {
  const { user, logout } = useAuth()
  const [mode, setMode] = useState<'paper' | 'broker'>('paper')
  const [paperPresets, setPaperPresets] = useState<Preset[]>([])
  const [brokerPresets, setBrokerPresets] = useState<Preset[]>([])
  const [status, setStatus] = useState<SetupStatus | null>(null)
  const [selectedPaperPreset, setSelectedPaperPreset] = useState('starter-eur')
  const [selectedBrokerPreset, setSelectedBrokerPreset] = useState('simulated-feed')
  const [customName, setCustomName] = useState('')
  const [externalAccountId, setExternalAccountId] = useState('')
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [needsReauth, setNeedsReauth] = useState(false)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const [paper, broker, st] = await Promise.all([
        demo.paperPresets(),
        demo.brokerPresets(),
        demo.status()
      ])
      setPaperPresets((paper ?? []).map((p: Record<string, unknown>) => normalizePreset(p)))
      setBrokerPresets((broker ?? []).map((p: Record<string, unknown>) => normalizePreset(p)))
      setStatus(normalizeStatus(st ?? {}))
    } catch (ex: any) {
      setError(parseError(ex?.message ?? 'Laden fehlgeschlagen'))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [])

  const selectedBroker = brokerPresets.find(p => p.id === selectedBrokerPreset)
  const brokerNeedsExternalId = selectedBrokerPreset === 'ib-paper' || selectedBrokerPreset === 'binance-testnet'

  async function handleQuickStart() {
    setBusy(true)
    setError(null)
    setSuccess(null)
    try {
      await demo.ensure()
      setSuccess('Demo-Börsenkonto wurde angelegt (100.000 € Starter Portfolio).')
      setNeedsReauth(user?.role === 'Viewer')
      await load()
      onChanged?.()
    } catch (ex: any) {
      setError(parseError(ex?.message ?? 'Anlegen fehlgeschlagen'))
    } finally {
      setBusy(false)
    }
  }

  async function handleProvision() {
    setBusy(true)
    setError(null)
    setSuccess(null)
    try {
      await demo.provision({
        presetId: selectedPaperPreset,
        customName: customName.trim() || undefined,
        isDefault: true
      })
      setSuccess('Paper-Konto erfolgreich angelegt.')
      setNeedsReauth(user?.role === 'Viewer')
      setCustomName('')
      await load()
      onChanged?.()
    } catch (ex: any) {
      setError(parseError(ex?.message ?? 'Anlegen fehlgeschlagen'))
    } finally {
      setBusy(false)
    }
  }

  async function handleLinkBroker() {
    setBusy(true)
    setError(null)
    setSuccess(null)
    try {
      await demo.linkBroker({
        presetId: selectedBrokerPreset,
        externalAccountId: externalAccountId.trim() || undefined
      })
      setSuccess('Demo-Broker wurde verknüpft.')
      setNeedsReauth(user?.role === 'Viewer')
      setExternalAccountId('')
      await load()
      onChanged?.()
    } catch (ex: any) {
      setError(parseError(ex?.message ?? 'Verknüpfung fehlgeschlagen'))
    } finally {
      setBusy(false)
    }
  }

  if (loading) {
    return <p className="text-sm text-gray-500">Lade Demo-Setup…</p>
  }

  return (
    <div className={`demo-setup ${compact ? 'demo-setup--compact' : ''}`}>
      {status && (
        <div className="demo-setup-status">
          <span className={`demo-setup-pill ${status.hasPaperAccount ? 'demo-setup-pill--ok' : ''}`}>
            Paper: {status.paperAccountCount}
          </span>
          <span className={`demo-setup-pill ${status.linkedBrokerCount > 0 ? 'demo-setup-pill--ok' : ''}`}>
            Broker: {status.linkedBrokerCount}
          </span>
          <span className="text-xs text-gray-500">{status.recommendedAction}</span>
        </div>
      )}

      {!status?.hasPaperAccount && (
        <div className="demo-quick-start">
          <p className="text-sm text-gray-300">Schnellstart mit Standard-Demo-Konto (100.000 €)</p>
          <button type="button" className="btn-primary" disabled={busy} onClick={handleQuickStart}>
            {busy ? 'Wird angelegt…' : 'Demo-Konto jetzt anlegen'}
          </button>
        </div>
      )}

      <div className="demo-setup-tabs">
        <button
          type="button"
          className={`demo-setup-tab ${mode === 'paper' ? 'demo-setup-tab--active' : ''}`}
          onClick={() => setMode('paper')}
        >
          Paper-Konto anlegen
        </button>
        <button
          type="button"
          className={`demo-setup-tab ${mode === 'broker' ? 'demo-setup-tab--active' : ''}`}
          onClick={() => setMode('broker')}
        >
          Demo-Broker anbinden
        </button>
      </div>

      {mode === 'paper' ? (
        <SectionCard
          title="Virtuelles Börsenkonto"
          description="Wähle ein Startkapital-Template. Orders und Setups nutzen dieses Paper-Konto."
        >
          <div className="demo-preset-grid">
            {paperPresets.map(p => (
              <button
                key={p.id}
                type="button"
                className={`demo-preset-card ${selectedPaperPreset === p.id ? 'demo-preset-card--active' : ''}`}
                onClick={() => setSelectedPaperPreset(p.id)}
              >
                <strong>{p.name}</strong>
                <span className="demo-preset-balance">{formatMoney(p.initialBalance, p.baseCurrency)}</span>
                <span className="demo-preset-desc">{p.description}</span>
              </button>
            ))}
          </div>
          <label className="block text-xs text-gray-500 mt-3 mb-1">Optionaler Kontoname</label>
          <input
            className="demo-input"
            value={customName}
            onChange={e => setCustomName(e.target.value)}
            placeholder="z. B. Mein Übungsdepot"
          />
          <button type="button" className="btn-primary mt-3" disabled={busy} onClick={handleProvision}>
            {busy ? 'Wird angelegt…' : 'Paper-Konto erstellen'}
          </button>
        </SectionCard>
      ) : (
        <SectionCard
          title="Demo-Broker verknüpfen"
          description="Speichert eine Demo-Verbindung für Marktdaten-Routing. Live-Trading bleibt deaktiviert."
        >
          <div className="demo-preset-grid">
            {brokerPresets.map(p => (
              <button
                key={p.id}
                type="button"
                className={`demo-preset-card ${selectedBrokerPreset === p.id ? 'demo-preset-card--active' : ''}`}
                onClick={() => setSelectedBrokerPreset(p.id)}
              >
                <strong>{p.name}</strong>
                <span className="demo-preset-desc">{p.description}</span>
              </button>
            ))}
          </div>
          {brokerNeedsExternalId && (
            <>
              <label className="block text-xs text-gray-500 mt-3 mb-1">
                Externe Demo Account-ID ({selectedBroker?.name})
              </label>
              <input
                className="demo-input"
                value={externalAccountId}
                onChange={e => setExternalAccountId(e.target.value)}
                placeholder="z. B. DU1234567"
              />
            </>
          )}
          <button type="button" className="btn-primary mt-3" disabled={busy} onClick={handleLinkBroker}>
            {busy ? 'Wird verknüpft…' : 'Demo-Broker anbinden'}
          </button>
        </SectionCard>
      )}

      {status && status.linkedBrokers.length > 0 && (
        <SectionCard title="Verknüpfte Demo-Broker" className="mt-4">
          <ul className="demo-broker-list">
            {status.linkedBrokers.map(b => (
              <li key={b.id} className="demo-broker-item">
                <div>
                  <strong>{b.name}</strong>
                  <span className="text-xs text-gray-500 block">{b.brokerName}</span>
                </div>
                <div className="text-right text-xs">
                  <span className="dash-badge">{b.connectionMode}</span>
                  {b.externalAccountId && <span className="block text-gray-500 mt-1">ID: {b.externalAccountId}</span>}
                </div>
              </li>
            ))}
          </ul>
        </SectionCard>
      )}

      {error && <div className="auth-error mt-3" role="alert">{error}</div>}
      {success && <div className="demo-success mt-3" role="status">{success}</div>}
      {needsReauth && (
        <div className="demo-reauth mt-3">
          Trading-Rechte wurden aktualisiert. Bitte einmal{' '}
          <button type="button" className="auth-link" onClick={logout}>neu anmelden</button>.
        </div>
      )}
    </div>
  )
}
