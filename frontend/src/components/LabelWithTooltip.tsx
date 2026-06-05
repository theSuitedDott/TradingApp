import React from 'react'
import Tooltip from './Tooltip'

type Props = {
  label: string
  tooltip: string
  htmlFor?: string
}

/** Form label with an attached help tooltip. */
export default function LabelWithTooltip({ label, tooltip, htmlFor }: Props) {
  return (
    <label htmlFor={htmlFor} className="block text-xs text-gray-400">
      <Tooltip text={tooltip}>{label}</Tooltip>
    </label>
  )
}
