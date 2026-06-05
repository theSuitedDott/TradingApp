import React, { createContext, useCallback, useContext, useMemo, useState } from 'react'

export type ToastVariant = 'success' | 'info' | 'warning' | 'error'

export type ToastItem = {
  id: string
  title: string
  message: string
  variant: ToastVariant
  href?: string
  createdAt: number
}

type ToastContextValue = {
  toasts: ToastItem[]
  showToast: (toast: Omit<ToastItem, 'id' | 'createdAt'>) => void
  dismissToast: (id: string) => void
}

const ToastContext = createContext<ToastContextValue | null>(null)

const AUTO_DISMISS_MS = 12_000

/** Provides stacked in-app toast notifications. */
export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([])

  const dismissToast = useCallback((id: string) => {
    setToasts(prev => prev.filter(t => t.id !== id))
  }, [])

  const showToast = useCallback((toast: Omit<ToastItem, 'id' | 'createdAt'>) => {
    const id = crypto.randomUUID()
    const item: ToastItem = { ...toast, id, createdAt: Date.now() }
    setToasts(prev => [item, ...prev].slice(0, 5))

    window.setTimeout(() => {
      dismissToast(id)
    }, AUTO_DISMISS_MS)
  }, [dismissToast])

  const value = useMemo(
    () => ({ toasts, showToast, dismissToast }),
    [toasts, showToast, dismissToast]
  )

  return <ToastContext.Provider value={value}>{children}</ToastContext.Provider>
}

/** Returns toast state and actions. */
export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast must be used within ToastProvider')
  return ctx
}
