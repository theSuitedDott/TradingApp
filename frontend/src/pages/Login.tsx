import React from 'react'
import { Navigate } from 'react-router-dom'
import { getToken } from '../api'

/** Legacy route — redirects to home; auth is handled by the overlay dialog. */
export default function Login() {
  if (getToken()) return <Navigate to="/" replace />
  return <Navigate to="/" replace />
}
