/** Authenticated user profile returned by the auth API. */
export type UserProfile = {
  id: string
  email: string
  role: string
  firstName?: string | null
  lastName?: string | null
  createdAt: string
}

/** Normalizes auth user payloads from camelCase or PascalCase JSON. */
export function normalizeUser(raw: Record<string, unknown>): UserProfile {
  return {
    id: String(raw.id ?? raw.Id ?? ''),
    email: String(raw.email ?? raw.Email ?? ''),
    role: String(raw.role ?? raw.Role ?? 'User'),
    firstName: (raw.firstName ?? raw.FirstName) as string | null | undefined,
    lastName: (raw.lastName ?? raw.LastName) as string | null | undefined,
    createdAt: String(raw.createdAt ?? raw.CreatedAt ?? new Date().toISOString())
  }
}
