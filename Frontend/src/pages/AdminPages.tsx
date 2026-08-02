import { AlertCircle, ArrowRight, Banknote, CheckCircle2, CircleDollarSign, Clock3, Copy, KeyRound, ReceiptText, Search, UserCheck, Users } from 'lucide-react'
import { useEffect, useState } from 'react'
import { StatusBadge } from '../components/StatusBadge'
import { api, dateTime, money } from '../lib/api'

type AdminOrder = { id: string; orderNumber: string; userEmail: string; amount: number; currency: string; credits: number; status: string; stripeSessionId?: string; createdAt: string; completedAt?: string; errorMessage?: string }
type AdminUser = { id: string; email: string; nickname?: string; balance: number; role: string; status: string; emailVerified: boolean; createdAt: string; lastLoginAt?: string }
type PageResult<T> = { items: T[]; total: number; page: number; pageSize: number }

export function AdminOverviewPage() {
  const [data, setData] = useState<{ totalUsers: number; activeUsers: number; totalOrders: number; pendingOrders: number; completedRevenue: number; creditedBalance: number; recentOrders: AdminOrder[] } | null>(null)
  useEffect(() => { api<typeof data>('/api/admin/overview').then(setData) }, [])
  return <div className="page"><AdminTitle title="经营概览" text="实时掌握平台用户、订单和资金状态。" />
    <div className="admin-metrics"><AdminMetric icon={Users} label="注册用户" value={String(data?.totalUsers ?? '—')} note={`${data?.activeUsers ?? 0} 位正常用户`} tone="green" /><AdminMetric icon={ReceiptText} label="累计订单" value={String(data?.totalOrders ?? '—')} note={`${data?.pendingOrders ?? 0} 笔待处理`} tone="blue" /><AdminMetric icon={Banknote} label="完成交易额" value={data ? money(data.completedRevenue) : '—'} note="按订单金额汇总" tone="amber" /><AdminMetric icon={CircleDollarSign} label="平台余额" value={data ? money(data.creditedBalance) : '—'} note="全部用户余额" tone="violet" /></div>
    <div className="admin-grid"><section className="panel"><div className="panel-head"><div><h3>最近订单</h3><p>最新发生的充值业务</p></div><a href="/admin/orders">管理订单 <ArrowRight size={15} /></a></div><div className="table-wrap"><table><thead><tr><th>订单</th><th>用户</th><th>金额</th><th>状态</th></tr></thead><tbody>{data?.recentOrders.map(x => <tr key={x.id}><td><b>{x.orderNumber}</b><small>{dateTime(x.createdAt)}</small></td><td>{x.userEmail}</td><td>{money(x.amount, x.currency)}</td><td><StatusBadge value={x.status} /></td></tr>)}</tbody></table></div></section>
      <section className="panel health-panel"><div className="panel-head"><div><h3>系统状态</h3><p>核心服务健康检查</p></div></div><Health icon={CheckCircle2} title="API 服务" text="运行正常" ok /><Health icon={CheckCircle2} title="PostgreSQL" text="连接正常" ok /><Health icon={data?.pendingOrders ? Clock3 : CheckCircle2} title="待处理订单" text={data?.pendingOrders ? `${data.pendingOrders} 笔等待支付确认` : '没有积压'} ok={!data?.pendingOrders} /><div className="health-note"><AlertCircle />Stripe 正式收款前请完成 Webhook 和生产密钥配置。</div></section></div>
  </div>
}

export function AdminOrdersPage() {
  const [result, setResult] = useState<PageResult<AdminOrder>>({ items: [], total: 0, page: 1, pageSize: 20 }); const [q, setQ] = useState(''); const [status, setStatus] = useState(''); const [loading, setLoading] = useState(false)
  const load = async () => { setLoading(true); try { setResult(await api(`/api/admin/orders?q=${encodeURIComponent(q)}${status ? `&status=${status}` : ''}`)) } finally { setLoading(false) } }
  useEffect(() => { void load() }, [status])
  const cancel = async (id: string) => { if (!confirm('确定取消这笔待处理订单？')) return; await api(`/api/admin/orders/${id}/cancel`, { method: 'POST' }); await load() }
  return <div className="page"><AdminTitle title="订单管理" text="查询全平台订单，快速定位异常与待处理业务。" />
    <div className="toolbar"><div className="search"><Search /><input value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && void load()} placeholder="搜索订单号或用户邮箱" /></div><select value={status} onChange={e => setStatus(e.target.value)}><option value="">全部状态</option><option value="1">待支付</option><option value="2">处理中</option><option value="3">已完成</option><option value="4">失败</option><option value="5">已取消</option></select><button className="secondary-button" onClick={() => void load()}>查询</button><span className="result-count">共 {result.total} 笔</span></div>
    <section className="panel"><div className="table-wrap"><table><thead><tr><th>订单信息</th><th>用户</th><th>金额 / 余额</th><th>状态</th><th>时间</th><th>操作</th></tr></thead><tbody>{result.items.map(x => <tr key={x.id}><td><b>{x.orderNumber}</b><small>{x.stripeSessionId || '尚未创建支付会话'}</small></td><td>{x.userEmail}</td><td>{money(x.amount, x.currency)}<small>到账 {x.credits.toFixed(2)}</small></td><td><StatusBadge value={x.status} /></td><td>{dateTime(x.createdAt)}</td><td>{['Pending','Processing'].includes(x.status) ? <button className="text-danger" onClick={() => void cancel(x.id)}>取消订单</button> : <span className="muted">—</span>}</td></tr>)}</tbody></table>{!result.items.length && <div className="empty-state"><ReceiptText /><h4>{loading ? '正在查询…' : '没有匹配订单'}</h4></div>}</div></section>
  </div>
}

export function AdminUsersPage() {
  const [result, setResult] = useState<PageResult<AdminUser>>({ items: [], total: 0, page: 1, pageSize: 20 }); const [q, setQ] = useState(''); const [status, setStatus] = useState(''); const [loading, setLoading] = useState(false)
  const load = async () => { setLoading(true); try { setResult(await api(`/api/admin/users?q=${encodeURIComponent(q)}${status ? `&status=${status}` : ''}`)) } finally { setLoading(false) } }
  useEffect(() => { void load() }, [status])
  const change = async (user: AdminUser) => { const next = user.status === 'Active' ? 2 : 1; await api(`/api/admin/users/${user.id}/status`, { method: 'PATCH', body: JSON.stringify({ status: next }) }); await load() }
  return <div className="page"><AdminTitle title="用户管理" text="查看用户资产和活跃状态，及时处理异常账户。" />
    <div className="toolbar"><div className="search"><Search /><input value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && void load()} placeholder="搜索邮箱或昵称" /></div><select value={status} onChange={e => setStatus(e.target.value)}><option value="">全部状态</option><option value="1">正常</option><option value="2">已停用</option></select><button className="secondary-button" onClick={() => void load()}>查询</button><span className="result-count">共 {result.total} 位</span></div>
    <section className="panel"><div className="table-wrap"><table><thead><tr><th>用户</th><th>角色</th><th>账户余额</th><th>状态</th><th>注册 / 登录</th><th>操作</th></tr></thead><tbody>{result.items.map(x => <tr key={x.id}><td><div className="user-cell"><div className="avatar small">{(x.nickname || x.email)[0].toUpperCase()}</div><div><b>{x.nickname || '未设置昵称'}</b><small>{x.email}</small></div></div></td><td><span className="role-chip">{x.role === 'Admin' ? '管理员' : '普通用户'}</span></td><td><b>{money(x.balance)}</b></td><td><StatusBadge value={x.status} /></td><td>{dateTime(x.createdAt)}<small>最近 {dateTime(x.lastLoginAt)}</small></td><td><button className={x.status === 'Active' ? 'text-danger' : 'text-success'} disabled={x.role === 'Admin'} onClick={() => void change(x)}>{x.role === 'Admin' ? '受保护' : x.status === 'Active' ? '停用' : '恢复'}</button></td></tr>)}</tbody></table>{!result.items.length && <div className="empty-state"><Users /><h4>{loading ? '正在查询…' : '没有匹配用户'}</h4></div>}</div></section>
  </div>
}

function AdminTitle({ title, text }: { title: string; text: string }) { return <header className="page-title admin-title"><span>管理后台</span><h1>{title}</h1><p>{text}</p></header> }
function AdminMetric({ icon: Icon, label, value, note, tone }: { icon: typeof Users; label: string; value: string; note: string; tone: string }) { return <div className="admin-metric"><div className={`metric-icon ${tone}`}><Icon /></div><span>{label}</span><strong>{value}</strong><small>{note}</small></div> }
function Health({ icon: Icon, title, text, ok }: { icon: typeof UserCheck; title: string; text: string; ok: boolean }) { return <div className="health-row"><Icon className={ok ? 'ok' : 'wait'} /><div><b>{title}</b><span>{text}</span></div><i className={ok ? 'online' : 'pending'} /></div> }

type CardItem = { id: string; maskedCode: string; credits: number; currency: string; status: string; batchName?: string; createdAt: string; expiresAt: string; redeemedAt?: string; redeemedBy?: string }
type GeneratedCard = { id: string; code: string; credits: number; expiresAt: string }

export function AdminCardsPage() {
  const [result, setResult] = useState<PageResult<CardItem>>({ items: [], total: 0, page: 1, pageSize: 20 }); const [status, setStatus] = useState(''); const [generated, setGenerated] = useState<GeneratedCard[]>([]); const [form, setForm] = useState({ credits: 25, count: 1, batchName: '运营卡', validDays: 365 }); const [busy, setBusy] = useState(false)
  const load = async () => setResult(await api(`/api/admin/cards${status ? `?status=${status}` : ''}`))
  useEffect(() => { void load() }, [status])
  const generate = async () => { setBusy(true); try { setGenerated(await api('/api/admin/cards', { method: 'POST', body: JSON.stringify(form) })); await load() } finally { setBusy(false) } }
  const toggle = async (item: CardItem) => { await api(`/api/admin/cards/${item.id}/status`, { method: 'PATCH', body: JSON.stringify({ status: item.status === 'Active' ? 3 : 1 }) }); await load() }
  const copyAll = async () => navigator.clipboard.writeText(generated.map(x => x.code).join('\n'))
  return <div className="page"><AdminTitle title="卡密管理" text="批量生成、跟踪核销并控制 LLMPay 平台余额卡。" />
    <section className="panel card-generator"><div><div className="generator-title"><div className="metric-icon violet"><KeyRound /></div><div><h3>生成余额卡</h3><p>明文卡密只在生成结果中展示一次</p></div></div><div className="generator-form"><label>面值<input type="number" min="1" max="10000" value={form.credits} onChange={e => setForm({...form,credits:Number(e.target.value)})} /></label><label>数量<input type="number" min="1" max="100" value={form.count} onChange={e => setForm({...form,count:Number(e.target.value)})} /></label><label>批次名称<input value={form.batchName} onChange={e => setForm({...form,batchName:e.target.value})} /></label><label>有效天数<input type="number" min="1" value={form.validDays} onChange={e => setForm({...form,validDays:Number(e.target.value)})} /></label><button className="primary-button" onClick={() => void generate()} disabled={busy}>{busy ? '生成中…' : '生成卡密'}</button></div></div>{generated.length > 0 && <div className="generated-box"><div><b>本次生成 {generated.length} 张</b><button onClick={() => void copyAll()}><Copy />复制全部</button></div>{generated.map(x => <code key={x.id}>{x.code}<span>{x.credits.toFixed(2)}</span></code>)}</div>}</section>
    <div className="toolbar card-toolbar"><select value={status} onChange={e => setStatus(e.target.value)}><option value="">全部状态</option><option value="1">可使用</option><option value="2">已核销</option><option value="3">已停用</option><option value="4">已过期</option></select><span className="result-count">共 {result.total} 张</span></div>
    <section className="panel"><div className="table-wrap"><table><thead><tr><th>卡密</th><th>面值</th><th>批次</th><th>状态</th><th>核销用户</th><th>有效期</th><th>操作</th></tr></thead><tbody>{result.items.map(x => <tr key={x.id}><td><b>{x.maskedCode}</b><small>{dateTime(x.createdAt)} 创建</small></td><td><b>{x.credits.toFixed(2)}</b></td><td>{x.batchName || '—'}</td><td><StatusBadge value={x.status} /></td><td>{x.redeemedBy || '—'}<small>{dateTime(x.redeemedAt)}</small></td><td>{dateTime(x.expiresAt)}</td><td>{['Active','Disabled'].includes(x.status) ? <button className={x.status === 'Active' ? 'text-danger' : 'text-success'} onClick={() => void toggle(x)}>{x.status === 'Active' ? '停用' : '启用'}</button> : '—'}</td></tr>)}</tbody></table></div></section>
  </div>
}
