export type User = {
  id: string; email: string; nickname?: string; balance: number; role: 'User' | 'Admin';
  status: string; emailVerified: boolean; createdAt: string
}

const TOKEN_KEY = 'llmpay_token'
export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (value: string) => localStorage.setItem(TOKEN_KEY, value),
  clear: () => localStorage.removeItem(TOKEN_KEY)
}

export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = tokenStore.get()
  const response = await fetch(path, {
    ...init,
    headers: {
      ...(init.body ? { 'Content-Type': 'application/json' } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers
    }
  })
  if (response.status === 401) {
    tokenStore.clear()
    if (!location.pathname.startsWith('/login')) location.assign('/login')
  }
  if (!response.ok) {
    const payload = await response.json().catch(() => ({}))
    throw new Error(payload.message || payload.error || `请求失败 (${response.status})`)
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export const money = (value: number, currency = 'USD') =>
  new Intl.NumberFormat('zh-CN', { style: 'currency', currency: currency.toUpperCase() }).format(value)

export const dateTime = (value?: string) => value ? new Intl.DateTimeFormat('zh-CN', {
  month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit'
}).format(new Date(value)) : '—'
