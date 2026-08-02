import { BarChart3, CircleDollarSign, KeyRound, LogOut, Menu, ReceiptText, TicketCheck, Users, X, Zap } from 'lucide-react'
import { useState } from 'react'
import type { ReactNode } from 'react'
import { useAuth } from '../auth/AuthContext'

const userLinks = [
  { to: '/app/cards', label: '充值套餐', icon: TicketCheck, end: false },
  { to: '/app/orders', label: '充值订单', icon: ReceiptText, end: false }
]
const adminLinks = [
  { to: '/admin', label: '经营概览', icon: BarChart3, end: true },
  { to: '/admin/orders', label: '订单管理', icon: ReceiptText },
  { to: '/admin/users', label: '用户管理', icon: Users }
  ,{ to: '/admin/cards', label: '卡密管理', icon: KeyRound }
]

export function Shell({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth()
  const [open, setOpen] = useState(false)
  const links = user?.role === 'Admin' ? [...userLinks, ...adminLinks] : userLinks
  return <div className={`app-shell ${user?.role === 'Admin' ? 'admin-shell' : 'customer-shell'}`}>
    <aside className={`sidebar ${open ? 'open' : ''}`}>
      <div className="brand"><span className="brand-mark">{user?.role === 'Admin' ? <CircleDollarSign size={22} /> : <Zap size={22}/>}</span><span>LLM<b>Pay</b></span></div>
      <button className="mobile-close" onClick={() => setOpen(false)} aria-label="关闭菜单"><X /></button>
      <nav>
        {links.map(({ to, label, icon: Icon, end }) => <a className={(end ? location.pathname === to : location.pathname.startsWith(to)) ? 'active' : ''} key={to} href={to} onClick={() => setOpen(false)}><Icon size={18} />{label}</a>)}
      </nav>
      <div className="sidebar-user">
        <div className="avatar">{(user?.nickname || user?.email || 'U').slice(0, 1).toUpperCase()}</div>
        <div><strong>{user?.nickname || '用户'}</strong><span>{user?.role === 'Admin' ? '平台管理员' : user?.email}</span></div>
        <button onClick={() => logout()} title="退出"><LogOut size={17} /></button>
      </div>
    </aside>
    {open && <button className="scrim" onClick={() => setOpen(false)} aria-label="关闭菜单" />}
    <main className="main"><header className="mobile-header"><button onClick={() => setOpen(true)}><Menu /></button><div className="brand compact"><CircleDollarSign size={20} /> LLMPay</div></header>{children}</main>
  </div>
}
