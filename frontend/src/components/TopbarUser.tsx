import React from 'react'
import { useAuth } from '../context/AuthContext'
import { formatDate, userDisplayName, userInitials } from '../utils/format'

/** Compact user badge for the top navigation bar. */
export default function TopbarUser() {
  const { user, userLoading } = useAuth()

  if (userLoading && !user) {
    return <div className="topbar-user topbar-user--loading">Lade Profil…</div>
  }

  if (!user) return null

  const name = userDisplayName(user)

  return (
    <div className="topbar-user" title={`${user.email} · ${user.role}`}>
      <span className="topbar-user-avatar" aria-hidden="true">{userInitials(user)}</span>
      <span className="topbar-user-info">
        <span className="topbar-user-name">{name}</span>
        <span className="topbar-user-meta">{user.role} · seit {formatDate(user.createdAt)}</span>
      </span>
    </div>
  )
}
