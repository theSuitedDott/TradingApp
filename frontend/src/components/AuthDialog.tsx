import React, { useEffect, useId, useState } from 'react'
import { auth, setToken } from '../api'

type Mode = 'login' | 'register'

type Props = {
  onSuccess: () => void
}

function parseAuthError(message: string): string {
  if (message.includes('401')) return 'E-Mail oder Passwort ist falsch.'
  if (message.includes('409')) return 'Diese E-Mail ist bereits registriert.'
  if (message.includes('400')) return 'Bitte prüfe deine Eingaben.'
  if (message.startsWith('HTTP')) return 'Verbindung zum Server fehlgeschlagen. Läuft das Backend?'
  return message
}

/** Full-screen login and registration overlay shown when no token is present. */
export default function AuthDialog({ onSuccess }: Props) {
  const formId = useId()
  const [mode, setMode] = useState<Mode>('login')
  const [email, setEmail] = useState('admin@tradingapp.local')
  const [password, setPassword] = useState('Admin123!ChangeMe')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const t = requestAnimationFrame(() => setVisible(true))
    return () => cancelAnimationFrame(t)
  }, [])

  function switchMode(next: Mode) {
    setMode(next)
    setError(null)
    if (next === 'register') {
      setEmail('')
      setPassword('')
    } else {
      setEmail('admin@tradingapp.local')
      setPassword('Admin123!ChangeMe')
    }
  }

  async function handleLogin(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const res = await auth.login({ email, password })
      setToken(res.accessToken ?? res.AccessToken)
      onSuccess()
    } catch (ex: any) {
      setError(parseAuthError(ex?.message ?? 'Anmeldung fehlgeschlagen'))
    } finally {
      setLoading(false)
    }
  }

  async function handleRegister(e: React.FormEvent) {
    e.preventDefault()
    if (password.length < 8) {
      setError('Das Passwort muss mindestens 8 Zeichen haben.')
      return
    }
    setError(null)
    setLoading(true)
    try {
      const res = await auth.register({ email, password, firstName, lastName })
      setToken(res.accessToken ?? res.AccessToken)
      onSuccess()
    } catch (ex: any) {
      setError(parseAuthError(ex?.message ?? 'Registrierung fehlgeschlagen'))
    } finally {
      setLoading(false)
    }
  }

  const isLogin = mode === 'login'

  return (
    <div
      className={`auth-overlay ${visible ? 'auth-overlay--visible' : ''}`}
      role="dialog"
      aria-modal="true"
      aria-labelledby={`${formId}-title`}
    >
      <div className="auth-backdrop" aria-hidden="true" />

      <div className={`auth-card ${visible ? 'auth-card--visible' : ''}`}>
        <div className="auth-brand">
          <div className="auth-logo" aria-hidden="true">TA</div>
          <div>
            <h1 id={`${formId}-title`} className="auth-title">TradingApp</h1>
            <p className="auth-subtitle">
              {isLogin
                ? 'Melde dich an, um Paper Trading und Setups zu nutzen.'
                : 'Erstelle ein Konto und starte mit virtuellem Trading.'}
            </p>
          </div>
        </div>

        <div className="auth-tabs" role="tablist">
          <button
            type="button"
            role="tab"
            aria-selected={isLogin}
            className={`auth-tab ${isLogin ? 'auth-tab--active' : ''}`}
            onClick={() => switchMode('login')}
          >
            Anmelden
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={!isLogin}
            className={`auth-tab ${!isLogin ? 'auth-tab--active' : ''}`}
            onClick={() => switchMode('register')}
          >
            Registrieren
          </button>
          <span className={`auth-tab-indicator ${isLogin ? 'auth-tab-indicator--left' : 'auth-tab-indicator--right'}`} />
        </div>

        <form
          className="auth-form"
          onSubmit={isLogin ? handleLogin : handleRegister}
          noValidate
        >
          <div className={`auth-fields ${isLogin ? 'auth-fields--login' : 'auth-fields--register'}`}>
            {!isLogin && (
              <div className="auth-field-row">
                <div className="auth-field">
                  <label htmlFor={`${formId}-first`}>Vorname</label>
                  <input
                    id={`${formId}-first`}
                    value={firstName}
                    onChange={e => setFirstName(e.target.value)}
                    placeholder="Max"
                    autoComplete="given-name"
                  />
                </div>
                <div className="auth-field">
                  <label htmlFor={`${formId}-last`}>Nachname</label>
                  <input
                    id={`${formId}-last`}
                    value={lastName}
                    onChange={e => setLastName(e.target.value)}
                    placeholder="Mustermann"
                    autoComplete="family-name"
                  />
                </div>
              </div>
            )}

            <div className="auth-field">
              <label htmlFor={`${formId}-email`}>E-Mail</label>
              <input
                id={`${formId}-email`}
                type="email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                placeholder="name@beispiel.de"
                autoComplete="email"
                required
              />
            </div>

            <div className="auth-field">
              <label htmlFor={`${formId}-password`}>Passwort</label>
              <div className="auth-password-wrap">
                <input
                  id={`${formId}-password`}
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  placeholder={isLogin ? '••••••••' : 'Mind. 8 Zeichen'}
                  autoComplete={isLogin ? 'current-password' : 'new-password'}
                  required
                  minLength={isLogin ? undefined : 8}
                />
                <button
                  type="button"
                  className="auth-password-toggle"
                  onClick={() => setShowPassword(v => !v)}
                  aria-label={showPassword ? 'Passwort verbergen' : 'Passwort anzeigen'}
                >
                  {showPassword ? 'Verbergen' : 'Anzeigen'}
                </button>
              </div>
            </div>
          </div>

          {isLogin && (
            <p className="auth-hint">
              Demo-Zugang: <code>admin@tradingapp.local</code> / <code>Admin123!ChangeMe</code>
            </p>
          )}

          {error && (
            <div className="auth-error" role="alert">
              {error}
            </div>
          )}

          <button type="submit" className="auth-submit" disabled={loading}>
            {loading ? (
              <span className="auth-submit-loading">
                <span className="auth-spinner" aria-hidden="true" />
                {isLogin ? 'Anmeldung läuft…' : 'Registrierung läuft…'}
              </span>
            ) : (
              isLogin ? 'Anmelden' : 'Konto erstellen'
            )}
          </button>
        </form>

        <p className="auth-footer">
          {isLogin ? (
            <>
              Noch kein Konto?{' '}
              <button type="button" className="auth-link" onClick={() => switchMode('register')}>
                Jetzt registrieren
              </button>
            </>
          ) : (
            <>
              Bereits registriert?{' '}
              <button type="button" className="auth-link" onClick={() => switchMode('login')}>
                Zur Anmeldung
              </button>
            </>
          )}
        </p>
      </div>
    </div>
  )
}
