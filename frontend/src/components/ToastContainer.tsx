import React from 'react'
import { Link } from 'react-router-dom'
import { useToast } from '../context/ToastContext'

/** Renders stacked toast notifications in the top-right corner. */
export default function ToastContainer() {
  const { toasts, dismissToast } = useToast()

  if (toasts.length === 0) return null

  return (
    <div className="toast-stack" aria-live="polite" aria-relevant="additions">
      {toasts.map(t => (
        <div key={t.id} className={`toast toast--${t.variant}`} role="status">
          <div className="toast-content">
            <strong className="toast-title">{t.title}</strong>
            <p className="toast-message">{t.message}</p>
            {t.href && (
              <Link to={t.href} className="toast-link" onClick={() => dismissToast(t.id)}>
                Zum Setup öffnen →
              </Link>
            )}
          </div>
          <button
            type="button"
            className="toast-close"
            aria-label="Benachrichtigung schließen"
            onClick={() => dismissToast(t.id)}
          >
            ×
          </button>
        </div>
      ))}
    </div>
  )
}
