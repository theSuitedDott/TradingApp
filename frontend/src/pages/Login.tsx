import React, { useState } from 'react'
import { auth, setToken } from '../api'

type Props = { onAuth: () => void }

export default function Login({ onAuth }: Props) {
  const [isRegister, setIsRegister] = useState(false)
  const [email, setEmail] = useState('admin@tradingapp.local')
  const [password, setPassword] = useState('Admin123!ChangeMe')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleLogin(e?: React.FormEvent) {
    e?.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const res = await auth.login({ email, password })
      setToken(res.accessToken)
      onAuth()
    } catch (ex: any) {
      setError(ex?.message ?? 'Login failed')
    } finally {
      setLoading(false)
    }
  }

  async function handleRegister(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const res = await auth.register({ email, password, firstName, lastName })
      // register returns AuthResponse with accessToken
      setToken(res.accessToken)
      onAuth()
    } catch (ex: any) {
      setError(ex?.message ?? 'Registration failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="login">
      <form onSubmit={isRegister ? handleRegister : handleLogin}>
        <h2 className="text-2xl font-semibold">{isRegister ? 'Registrieren' : 'Anmelden'}</h2>

        {isRegister && (
          <>
            <div>
              <label className="text-sm">Vorname</label>
              <input className="w-full p-2 rounded bg-gray-700 border border-gray-600" value={firstName} onChange={e => setFirstName(e.target.value)} />
            </div>
            <div>
              <label className="text-sm">Nachname</label>
              <input className="w-full p-2 rounded bg-gray-700 border border-gray-600" value={lastName} onChange={e => setLastName(e.target.value)} />
            </div>
          </>
        )}

        <div>
          <label className="text-sm">Email</label>
          <input className="w-full p-2 rounded bg-gray-700 border border-gray-600" value={email} onChange={e => setEmail(e.target.value)} />
        </div>

        <div>
          <label className="text-sm">Password</label>
          <input className="w-full p-2 rounded bg-gray-700 border border-gray-600" type="password" value={password} onChange={e => setPassword(e.target.value)} />
        </div>

        <div className="flex items-center justify-between">
          <button className="px-4 py-2 bg-primary rounded text-black mt-2" type="submit" disabled={loading}>
            {isRegister ? (loading ? 'Registriere...' : 'Registrieren') : (loading ? 'Anmelden...' : 'Login')}
          </button>
          <button
            type="button"
            className="text-sm text-gray-400 underline ml-4"
            onClick={() => { setIsRegister(!isRegister); setError(null) }}
          >
            {isRegister ? 'Zurück zum Login' : 'Neu? Registrieren'}
          </button>
        </div>

        {error && <div className="mt-2 text-red-400">{error}</div>}
      </form>
    </div>
  )
}

