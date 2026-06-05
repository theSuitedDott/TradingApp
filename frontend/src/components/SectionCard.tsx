import React from 'react'
import Tooltip from './Tooltip'

type Props = {
  title: string
  description?: string
  tooltip?: string
  children: React.ReactNode
  className?: string
}

/** Card section with title, optional description and tooltip. */
export default function SectionCard({ title, description, tooltip, children, className = '' }: Props) {
  return (
    <section className={`card ${className}`}>
      <div className="mb-4">
        <h3 className="font-medium text-base">
          {tooltip ? <Tooltip text={tooltip}>{title}</Tooltip> : title}
        </h3>
        {description && <p className="text-sm text-gray-400 mt-1">{description}</p>}
      </div>
      {children}
    </section>
  )
}
