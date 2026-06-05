import { useCallback, useEffect, useRef } from 'react'

const BLINK_INTERVAL_MS = 800

/** Blinks the browser tab title until the user focuses the window again. */
export function useTabBlink() {
  const originalTitleRef = useRef(typeof document !== 'undefined' ? document.title : 'TradingApp')
  const intervalRef = useRef<number | null>(null)
  const showAlertRef = useRef(true)

  const stopBlink = useCallback(() => {
    if (intervalRef.current !== null) {
      window.clearInterval(intervalRef.current)
      intervalRef.current = null
    }
    document.title = originalTitleRef.current
  }, [])

  const startBlink = useCallback((alertTitle: string) => {
    originalTitleRef.current = document.title
    showAlertRef.current = true
    stopBlink()

    intervalRef.current = window.setInterval(() => {
      document.title = showAlertRef.current ? alertTitle : originalTitleRef.current
      showAlertRef.current = !showAlertRef.current
    }, BLINK_INTERVAL_MS)
  }, [stopBlink])

  useEffect(() => {
    const onFocus = () => stopBlink()
    const onVisibility = () => {
      if (!document.hidden) stopBlink()
    }

    window.addEventListener('focus', onFocus)
    document.addEventListener('visibilitychange', onVisibility)
    return () => {
      window.removeEventListener('focus', onFocus)
      document.removeEventListener('visibilitychange', onVisibility)
      stopBlink()
    }
  }, [stopBlink])

  return { startBlink, stopBlink }
}
