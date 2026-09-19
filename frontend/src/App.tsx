import React, { useEffect, useState } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import Dashboard from './pages/Dashboard'
import Strategies from './pages/Strategies'
import Backtesting from './pages/Backtesting'
import Paper from './pages/Paper'
import Setups from './pages/Setups'
import Settings from './pages/Settings'
import Sidebar from './components/Sidebar'
import AuthDialog from './components/AuthDialog'
import ToastContainer from './components/ToastContainer'
import TopbarUser from './components/TopbarUser'
import { AuthProvider, useAuth } from './context/AuthContext'
import { SetupNotificationProvider, useSetupNotifications } from './context/SetupNotificationContext'
import { ToastProvider } from './context/ToastContext'
import { requestNotificationPermission } from './utils/browserNotify'

function NotificationPermissionButton() {
  const [permission, setPermission] = useState<NotificationPermission | 'unsupported'>(
    'Notification' in window ? Notification.permission : 'unsupported'
  )

  useEffect(() => {
    if (!('Notification' in window)) return
    setPermission(Notification.permission)
  }, [])

  if (permission === 'granted' || permission === 'unsupported') return null

  return (
    <button
      type="button"
      className="topbar-notify-btn"
      title="Browser-Benachrichtigungen aktivieren (erscheinen auch wenn Tab im Hintergrund ist)"
      onClick={async () => {
        const granted = await requestNotificationPermission()
        setPermission(granted ? 'granted' : 'denied')
      }}
    >
      🔔 Benachrichtigungen aktivieren
    </button>
  )
}

function AppShell() {
  const { isAuthenticated, onAuthSuccess, logout } = useAuth()
  const { alertActive } = useSetupNotifications()

  return (
    <div className={`app ${!isAuthenticated ? 'app--locked' : ''} ${alertActive ? 'app--setup-alert' : ''}`}>
      <header className="topbar">
        <h1 className="text-lg font-semibold">TradingApp</h1>
        {isAuthenticated && (
          <div className="topbar-actions">
            <NotificationPermissionButton />
            <TopbarUser />
            <button
            type="button"
            className="topbar-logout-btn"
            onClick={logout}
            title="Abmelden und Anmeldedialog anzeigen"
          >
            <svg className="topbar-logout-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0 0 13.5 3h-6a2.25 2.25 0 0 0-2.25 2.25v13.5A2.25 2.25 0 0 0 7.5 21h6a2.25 2.25 0 0 0 2.25-2.25V15" />
              <path strokeLinecap="round" strokeLinejoin="round" d="m18 12-3-3m3 3 3-3m-3 3V9" />
            </svg>
            Abmelden
          </button>
          </div>
        )}
      </header>
      <div className="dashboard">
        <Sidebar />
        <main className="content">
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/strategies" element={<Strategies />} />
            <Route path="/backtesting" element={<Backtesting />} />
            <Route path="/paper" element={<Paper />} />
            <Route path="/setups" element={<Setups />} />
            <Route path="/settings" element={<Settings />} />
            <Route path="/login" element={<Navigate to="/" replace />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </main>
      </div>
      {!isAuthenticated && <AuthDialog onSuccess={onAuthSuccess} />}
      <ToastContainer />
    </div>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <ToastProvider>
        <SetupNotificationProvider>
          <AppShell />
        </SetupNotificationProvider>
      </ToastProvider>
    </AuthProvider>
  )
}
