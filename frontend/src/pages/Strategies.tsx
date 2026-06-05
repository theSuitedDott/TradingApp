import React from 'react'

const mock = [
  { id: 'sma-1', name: 'SMA Crossover', description: 'Fast/Slow SMA strategy', version: 1 },
  { id: 'rsi-1', name: 'RSI Mean Reversion', description: 'RSI-based entries', version: 1 }
]

export default function Strategies() {
  return (
    <div className="space-y-4">
      <h2 className="text-2xl font-semibold">Strategien</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {mock.map(s => (
          <div key={s.id} className="card">
            <h3 className="text-lg font-medium">{s.name}</h3>
            <p className="text-gray-400">{s.description}</p>
            <div className="mt-3 text-sm text-gray-300">Version {s.version}</div>
          </div>
        ))}
      </div>
    </div>
  )
}

