import { useAuth } from './auth/AuthContext'
import { Shell } from './components/Shell'
import { LoginPage, RegisterPage, SetPasswordPage } from './pages/AuthPages'
import { DashboardPage, OrdersPage, RechargePage } from './pages/UserPages'
import { AdminCardsPage, AdminOrdersPage, AdminOverviewPage, AdminUsersPage } from './pages/AdminPages'
import { CardFlowPage } from './pages/CardFlowPage'
import { SubscriptionOrdersPage } from './pages/SubscriptionOrdersPage'

export default function App() {
  const { user, loading } = useAuth()
  const path = location.pathname
  if (path === '/login') return <LoginPage />
  if (path === '/register') return <RegisterPage />
  if (path === '/set-password') return <SetPasswordPage />
  if (loading) return <div className="screen-loader"><span className="spinner" />正在加载账户…</div>
  if (path.startsWith('/app/cards')) return <CardFlowPage />
  if (!user) { location.replace('/login'); return null }
  if (path.startsWith('/admin') && user.role !== 'Admin') { location.replace('/app'); return null }
  if (path === '/app/orders') return <SubscriptionOrdersPage />
  if (path === '/app' && user.role !== 'Admin') { location.replace('/app/orders'); return null }

  const page = path === '/app/recharge' ? <RechargePage />
    : path === '/admin/orders' ? <AdminOrdersPage />
    : path === '/admin/users' ? <AdminUsersPage />
    : path === '/admin/cards' ? <AdminCardsPage />
    : path === '/admin' ? <AdminOverviewPage />
    : <DashboardPage />
  return <Shell>{page}</Shell>
}
