import { useCallback, useState } from 'react'

export const SETUP_DISPLAY_LIMIT_KEY = 'ta-setup-display-limit'

export const SETUP_DISPLAY_LIMIT_OPTIONS = [
  { value: 3, label: '3' },
  { value: 5, label: '5' },
  { value: 10, label: '10' },
  { value: 20, label: '20' },
  { value: 0, label: 'Alle' }
] as const

function readStoredLimit(): number {
  const raw = localStorage.getItem(SETUP_DISPLAY_LIMIT_KEY)
  const parsed = raw ? Number(raw) : 10
  return SETUP_DISPLAY_LIMIT_OPTIONS.some(o => o.value === parsed) ? parsed : 10
}

/** Persists how many detected opportunities are shown on the Setups page. */
export function useSetupDisplayLimit() {
  const [limit, setLimitState] = useState(readStoredLimit)

  const setLimit = useCallback((value: number) => {
    setLimitState(value)
    localStorage.setItem(SETUP_DISPLAY_LIMIT_KEY, String(value))
  }, [])

  const applyLimit = useCallback(<T,>(items: T[]): T[] => {
    if (limit <= 0) return items
    return items.slice(0, limit)
  }, [limit])

  return { limit, setLimit, applyLimit }
}
