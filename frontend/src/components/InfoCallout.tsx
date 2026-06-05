import React from 'react'

type Props = {
  title: string
  children: React.ReactNode
  variant?: 'info' | 'success' | 'warning'
}

/** Highlighted info panel for onboarding and explanations. */
export default function InfoCallout({ title, children, variant = 'info' }: Props) {
  const styles = {
    info: 'border-blue-800/60 bg-blue-950/30 text-blue-100',
    success: 'border-emerald-800/60 bg-emerald-950/30 text-emerald-100',
    warning: 'border-amber-800/60 bg-amber-950/30 text-amber-100'
  }[variant]

  return (
    <div className={`card border ${styles}`}>
      <h3 className="font-medium text-sm mb-2">{title}</h3>
      <div className="text-sm leading-relaxed opacity-90 space-y-2">{children}</div>
    </div>
  )
}
