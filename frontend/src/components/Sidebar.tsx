import React from 'react'
import { NavLink } from 'react-router-dom'

const items = [
  { to: '/', label: 'Dashboard', hint: 'Kontoübersicht, Positionen & manuelle Orders' },
  { to: '/setups', label: 'Setups', hint: 'Signal-Analyse, Kerzenchart, Backtest & Trade-Ausführung' },
  { to: '/strategies', label: 'Strategien', hint: 'Strategie-Konfiguration (Platzhalter)' },
  { to: '/backtesting', label: 'Backtesting', hint: 'Historische Strategie-Tests (Platzhalter)' },
  { to: '/paper', label: 'Paper Trading', hint: 'Virtuelle Konten, Orders & Positionen' },
  { to: '/settings', label: 'Einstellungen', hint: 'App- und Benutzereinstellungen' }
]

export default function Sidebar() {
  return (
    <aside className="sidebar">
      <div className="mb-4">
        <h2 className="text-xl font-bold">TradingApp</h2>
        <p className="text-sm text-gray-400">Paper Trading UI</p>
      </div>
      <nav className="space-y-2">
        {items.map(i => (
          <NavLink
            key={i.to}
            to={i.to}
            title={i.hint}
            className={({ isActive }) =>
              `block px-3 py-2 rounded ${isActive ? 'bg-gray-700 text-white' : 'text-gray-300 hover:bg-gray-800'}`
            }
          >
            <span className="font-medium">{i.label}</span>
            <span className="block text-[11px] text-gray-500 mt-0.5 leading-tight">{i.hint}</span>
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}

