import React from 'react'

const mock = [
  { id: 'bt-1', name: 'SMA Backtest', period: '2020-2023', result: 'Sharpe 1.2' },
  { id: 'bt-2', name: 'RSI Backtest', period: '2021-2023', result: 'Sharpe 0.8' }
]

export default function Backtesting() {
  return (
    <div>
      <h2 className="text-2xl font-semibold">Backtesting</h2>
      <div className="mt-4 grid gap-4">
        {mock.map(b => (
          <div key={b.id} className="card flex items-center justify-between">
            <div>
              <h3 className="font-medium">{b.name}</h3>
              <div className="text-sm text-gray-400">{b.period}</div>
            </div>
            <div className="text-right">
              <div className="font-semibold">{b.result}</div>
              <button className="mt-2 px-3 py-1 bg-accent rounded text-white text-sm">Details</button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

