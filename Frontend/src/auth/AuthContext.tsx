import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api, tokenStore, type User } from '../lib/api'

type AuthValue = {
  user: User | null; loading: boolean;
  login: (email: string, password: string) => Promise<User>;
  register: (email: string, password: string, nickname: string) => Promise<void>;
  refresh: () => Promise<void>; logout: (redirectTo?: string) => void
}

const AuthContext = createContext<AuthValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  const refresh = async () => {
    if (!tokenStore.get()) { setUser(null); setLoading(false); return }
    try { setUser(await api<User>('/api/auth/me')) } catch { setUser(null) } finally { setLoading(false) }
  }
  useEffect(() => { void refresh() }, [])

  const login = async (email: string, password: string) => {
    const result = await api<{ token: string; user: User }>('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })
    tokenStore.set(result.token); setUser(result.user); return result.user
  }
  const register = async (email: string, password: string, nickname: string) => {
    await api('/api/auth/register', { method: 'POST', body: JSON.stringify({ email, password, nickname }) })
  }
  const logout = (redirectTo = '/login') => {
    tokenStore.clear()
    localStorage.removeItem('llmpay_guest_order_access')
    localStorage.removeItem('llmpay_mock_recharge_order')
    setUser(null)
    location.replace(redirectTo)
  }

  return <AuthContext.Provider value={{ user, loading, login, register, refresh, logout }}>{children}</AuthContext.Provider>
}

export const useAuth = () => {
  const value = useContext(AuthContext)
  if (!value) throw new Error('AuthProvider is missing')
  return value
}
