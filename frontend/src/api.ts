const API_URL = import.meta.env.VITE_API_URL ?? ''

export function setToken(token: string) {
  localStorage.setItem('ta_token', token)
}

export function getToken(): string | null {
  return localStorage.getItem('ta_token')
}

export function logout() {
  localStorage.removeItem('ta_token')
}

async function request(path: string, options: RequestInit = {}) {
  const token = getToken()
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
  login: (payload: any) => request('/api/v1/auth/login', { method: 'POST', body: JSON.stringify(payload) })
}

export const accounts = {
  list: () => request('/api/v1/paper/accounts'),
  create: (payload: any) => request('/api/v1/paper/accounts', { method: 'POST', body: JSON.stringify(payload) }),
  portfolio: (accountId: string) => request(`/api/v1/paper/accounts/${accountId}/portfolio`)
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

