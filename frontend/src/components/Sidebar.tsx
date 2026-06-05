import React from 'react'
import { NavLink } from 'react-router-dom'

const items = [
  { to: '/', label: 'Dashboard' },
  { to: '/strategies', label: 'Strategien' },
  { to: '/backtesting', label: 'Backtesting' },
  { to: '/paper', label: 'Paper Trading' },
  { to: '/settings', label: 'Einstellungen' }
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
          <NavLink key={i.to} to={i.to} className={({isActive}) => `block px-3 py-2 rounded ${isActive ? 'bg-gray-700 text-white' : 'text-gray-300 hover:bg-gray-800'}`}>
            {i.label}
          </NavLink>
        ))}
      </nav>
      <div className="mt-auto pt-6 text-sm text-gray-500">
        <div>Dark Theme</div>
        <div className="mt-2">Responsive • Modern</div>
      </div>
    </aside>
  )
}

