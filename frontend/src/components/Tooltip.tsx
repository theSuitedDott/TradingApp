import React, { useId } from 'react'

type Props = {
  text: string
  children: React.ReactNode
  className?: string
}

/** Hover tooltip for inline help text. */
export default function Tooltip({ text, children, className = '' }: Props) {
  const id = useId()
  return (
    <span className={`tooltip-wrap inline-flex items-center gap-1 ${className}`}>
      {children}
      <button
        type="button"
        className="tooltip-trigger"
        aria-describedby={id}
        aria-label="Hilfe anzeigen"
      >
        ?
      </button>
      <span id={id} role="tooltip" className="tooltip-bubble">
        {text}
      </span>
    </span>
  )
}
