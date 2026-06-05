import React, { useEffect, useState } from 'react'
import { Routes, Route, Navigate, useNavigate } from 'react-router-dom'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
import Strategies from './pages/Strategies'
import Backtesting from './pages/Backtesting'
import Paper from './pages/Paper'
import Settings from './pages/Settings'
import { getToken, logout } from './api'
import Sidebar from './components/Sidebar'

function RequireAuth({ children }: { children: JSX.Element }) {
  const token = getToken()
  if (!token) {
    return <Navigate to="/login" replace />
  }
  return children
}

export default function App() {
  return (
    <div className="app">
      <header className="topbar">
        <h1 className="text-lg font-semibold">TradingApp</h1>
      </header>
      <div className="dashboard">
        <Sidebar />
        <main className="content">
          <Routes>
            <Route path="/login" element={<Login onAuth={() => window.location.replace('/')} />} />
            <Route path="/" element={<RequireAuth><Dashboard /></RequireAuth>} />
            <Route path="/strategies" element={<RequireAuth><Strategies /></RequireAuth>} />
            <Route path="/backtesting" element={<RequireAuth><Backtesting /></RequireAuth>} />
            <Route path="/paper" element={<RequireAuth><Paper /></RequireAuth>} />
            <Route path="/settings" element={<RequireAuth><Settings /></RequireAuth>} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </main>
      </div>
    </div>
  )
}

