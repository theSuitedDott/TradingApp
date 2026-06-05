import React from 'react'

export default function Settings() {
  return (
    <div>
      <h2 className="text-2xl font-semibold">Einstellungen</h2>
      <div className="mt-4 grid gap-4">
        <div className="card">
          <h3 className="font-medium">Theme</h3>
          <div className="text-sm text-gray-400">Dark mode (default)</div>
        </div>
        <div className="card">
          <h3 className="font-medium">API</h3>
          <div className="text-sm text-gray-400">Configure API endpoint / keys in environment</div>
        </div>
      </div>
    </div>
  )
}

