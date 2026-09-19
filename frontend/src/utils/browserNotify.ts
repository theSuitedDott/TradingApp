/**
 * Browser Push Notification helper.
 * Requests permission once and shows native OS notifications
 * even when the browser tab is minimised or in the background.
 */

/** Requests notification permission if not already granted. Returns true when allowed. */
export async function requestNotificationPermission(): Promise<boolean> {
  if (!('Notification' in window)) return false
  if (Notification.permission === 'granted') return true
  if (Notification.permission === 'denied') return false
  const result = await Notification.requestPermission()
  return result === 'granted'
}

/** Returns true when the browser supports and has granted notification permission. */
export function notificationsGranted(): boolean {
  return 'Notification' in window && Notification.permission === 'granted'
}

export type BrowserNotifyOptions = {
  title: string
  body: string
  /** 'buy' = green dot, 'sell-sl' = red warning, 'sell-tp' = green flag */
  type: 'buy' | 'sell-sl' | 'sell-tp'
}

const ICONS: Record<BrowserNotifyOptions['type'], string> = {
  buy:     'data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><circle cx="16" cy="16" r="16" fill="%2310b981"/><text x="16" y="21" font-size="18" text-anchor="middle" fill="white">▲</text></svg>',
  'sell-sl': 'data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><circle cx="16" cy="16" r="16" fill="%23ef4444"/><text x="16" y="21" font-size="18" text-anchor="middle" fill="white">▼</text></svg>',
  'sell-tp': 'data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><circle cx="16" cy="16" r="16" fill="%2310b981"/><text x="16" y="21" font-size="18" text-anchor="middle" fill="white">✓</text></svg>',
}

/**
 * Shows a native OS notification if permission is granted.
 * Falls back silently when notifications are not available.
 */
export function showBrowserNotification({ title, body, type }: BrowserNotifyOptions): void {
  if (!notificationsGranted()) return
  try {
    const n = new Notification(title, {
      body,
      icon: ICONS[type],
      tag: `tradingapp-${type}`,   // same tag = replaces previous, no spam
      renotify: true,
    })
    // Auto-close after 10 seconds
    window.setTimeout(() => n.close(), 10_000)
  } catch {
    // Notifications blocked at OS level — ignore
  }
}
