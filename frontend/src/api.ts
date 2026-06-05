const API_URL = import.meta.env.VITE_API_URL ?? ''

export const apiBaseUrl = API_URL

export function setToken(token: string) {
  localStorage.setItem('ta_token', token)
}

export function getToken(): string | null {
  return localStorage.getItem('ta_token')
}

export function logout() {
  localStorage.removeItem('ta_token')
}

type RequestConfig = { sendAuth?: boolean }

async function request(path: string, options: RequestInit = {}, config: RequestConfig = {}) {
  const sendAuth = config.sendAuth !== false
  const token = sendAuth ? getToken() : null
  const headers: Record<string,string> = {
    'Content-Type': 'application/json'
  }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const url = API_URL ? API_URL + path : path
  const res = await fetch(url, {
    ...options,
    headers: { ...headers, ...(options.headers as any) }
  })
  if (!res.ok) {
    const txt = await res.text()
    throw new Error(`HTTP ${res.status}: ${txt}`)
  }
  return res.json().catch(() => null)
}

export const auth = {
  register: (payload: any) => request('/api/v1/auth/register', { method: 'POST', body: JSON.stringify(payload) }),
  login: (payload: any) => request('/api/v1/auth/login', { method: 'POST', body: JSON.stringify(payload) }),
  me: () => request('/api/v1/auth/me')
}

export const accounts = {
  list: () => request('/api/v1/paper/accounts'),
  create: (payload: any) => request('/api/v1/paper/accounts', { method: 'POST', body: JSON.stringify(payload) }),
  portfolio: (accountId: string) => request(`/api/v1/paper/accounts/${accountId}/portfolio`),
  positions: (accountId: string, openOnly = true) =>
    request(`/api/v1/paper/accounts/${accountId}/positions?openOnly=${openOnly}`)
}

export const demo = {
  paperPresets: () => request('/api/v1/paper/demo/presets/paper'),
  brokerPresets: () => request('/api/v1/paper/demo/presets/broker'),
  status: () => request('/api/v1/paper/demo/status'),
  provision: (payload: { presetId: string; customName?: string; isDefault?: boolean }) =>
    request('/api/v1/paper/demo/provision', { method: 'POST', body: JSON.stringify(payload) }),
  ensure: () => request('/api/v1/paper/demo/ensure', { method: 'POST' }),
  linkBroker: (payload: { presetId: string; displayName?: string; externalAccountId?: string }) =>
    request('/api/v1/paper/demo/link-broker', { method: 'POST', body: JSON.stringify(payload) }),
  brokers: () => request('/api/v1/paper/demo/brokers')
}

export const orders = {
  place: (accountId: string, payload: any) => request(`/api/v1/paper/accounts/${accountId}/orders`, { method: 'POST', body: JSON.stringify(payload) }),
  list: (accountId: string) => request(`/api/v1/paper/accounts/${accountId}/orders`),
  cancel: (accountId: string, orderId: string) => request(`/api/v1/paper/accounts/${accountId}/orders/${orderId}`, { method: 'DELETE' })
}

export const market = {
  submitQuote: (payload: any) => request('/api/v1/market/quotes', { method: 'POST', body: JSON.stringify(payload) }),
  getQuote: (symbol: string, exchange: string) => request(`/api/v1/market/quotes/${symbol}/${exchange}`)
}

const setupsPublic = { sendAuth: false } as const

export const setups = {
  analyze: (symbol?: string, exchange?: string) => {
    const params = new URLSearchParams()
    if (symbol) params.set('symbol', symbol)
    if (exchange) params.set('exchange', exchange)
    const query = params.toString()
    return request(`/api/v1/setups/analyze${query ? `?${query}` : ''}`, {}, setupsPublic)
  },
  backtest: (symbol: string) => request(`/api/v1/setups/backtest?symbol=${symbol}`, {}, setupsPublic),
  candles: (params: {
    symbol: string
    interval?: string
    range?: string
    mock?: boolean
    live?: boolean
    includeLevels?: boolean
  }) => {
    const q = new URLSearchParams({ symbol: params.symbol })
    if (params.interval) q.set('interval', params.interval)
    if (params.range) q.set('range', params.range)
    if (params.mock) q.set('mock', 'true')
    if (params.live) q.set('live', 'true')
    if (params.includeLevels) q.set('includeLevels', 'true')
    return request(`/api/v1/setups/candles?${q}`, {}, setupsPublic)
  },
  opportunities: () => request('/api/v1/setups/opportunities', {}, setupsPublic),
  execute: (opportunityId: string, payload: { accountId: string; quantity: number }) =>
    request(`/api/v1/setups/opportunities/${opportunityId}/execute`, {
      method: 'POST',
      body: JSON.stringify(payload)
    })
}

