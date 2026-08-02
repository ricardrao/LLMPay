import { ArrowRight, Check, CircleDollarSign, Clock3, CreditCard, History, ReceiptText, ShieldCheck, Sparkles, WalletCards } from 'lucide-react'
import { FormEvent, useEffect, useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { StatusBadge } from '../components/StatusBadge'
import { api, dateTime, money } from '../lib/api'

type Order = { orderNumber: string; amount: number; currency: string; credits: number; status: string; createdAt: string; completedAt?: string }
type Transaction = { id: string; amount: number; balanceAfter: number; type: string | number; referenceId: string; description?: string; createdAt: string }
type Wallet = { balance: number; transactions: Transaction[] }
type SubscriptionOrder = { id: string; orderNumber: string; targetEmail: string; targetName: string; plan: 'Plus'|'Credit100'|'Pro'; paymentProvider: 'wechat'|'alipay'; amount: number; status: string; createdAt: string; completedAt?: string }

const statusName = (value: string | number) => typeof value === 'number' ? ['','Pending','Processing','Completed','Failed','Cancelled','Refunding','Refunded'][value] : value

export function DashboardPage() {
  const { user, refresh } = useAuth(); const [wallet, setWallet] = useState<Wallet | null>(null); const [orders, setOrders] = useState<Order[]>([]); const [loading, setLoading] = useState(true)
  useEffect(() => { Promise.all([api<Wallet>('/api/payments/wallet'), api<Order[]>('/api/payments/orders')]).then(([w, o]) => { setWallet(w); setOrders(o); void refresh() }).finally(() => setLoading(false)) }, [])
  return <div className="page"><PageTitle eyebrow="账户中心" title={`你好，${user?.nickname || '欢迎回来'}`} text="这里是你的余额与订单总览。" />
    <div className="hero-balance"><div><span>可用平台余额</span><strong>{loading ? '—' : money(wallet?.balance || 0)}</strong><p><ShieldCheck size={15} /> 资金流水实时记录，安全可追溯</p></div><a className="light-button" href="/app/recharge">立即充值 <ArrowRight size={17} /></a><div className="balance-orb" /></div>
    <div className="metric-grid"><Metric icon={ReceiptText} label="累计订单" value={String(orders.length)} hint="全部充值记录" /><Metric icon={Check} label="成功充值" value={String(orders.filter(x => statusName(x.status) === 'Completed').length)} hint="已完成订单" /><Metric icon={Clock3} label="处理中" value={String(orders.filter(x => ['Pending','Processing'].includes(statusName(x.status))).length)} hint="等待支付确认" /></div>
    <section className="panel"><div className="panel-head"><div><h3>最近订单</h3><p>查看近期充值进度</p></div><a href="/app/orders">查看全部 <ArrowRight size={15} /></a></div><OrderTable orders={orders.slice(0, 5)} empty="还没有充值订单" /></section>
  </div>
}

export function RechargePage() {
  const { user } = useAuth(); const [amount, setAmount] = useState(25); const [currency, setCurrency] = useState('usd'); const [busy, setBusy] = useState(false); const [error, setError] = useState('')
  const submit = async (e: FormEvent) => { e.preventDefault(); setBusy(true); setError(''); try { const result = await api<{ url: string }>('/api/payments/recharge-checkout', { method: 'POST', body: JSON.stringify({ amount, currency }) }); location.assign(result.url) } catch (ex) { setError((ex as Error).message) } finally { setBusy(false) } }
  return <div className="page narrow"><PageTitle eyebrow="余额充值" title="为账户补充余额" text="选择金额后由 Stripe 安全完成支付，到账状态自动同步。" />
    <div className="recharge-layout"><form className="panel recharge-card" onSubmit={submit}><div className="field-row"><label>支付币种<select value={currency} onChange={e => setCurrency(e.target.value)}><option value="usd">USD · 美元</option><option value="cny">CNY · 人民币</option></select></label></div><div className="amount-label"><span>选择充值金额</span><small>当前余额 {money(user?.balance || 0)}</small></div><div className="amount-grid">{[10,25,50,100,250,500].map(value => <button type="button" className={amount === value ? 'selected' : ''} onClick={() => setAmount(value)} key={value}><span>{currency === 'usd' ? '$' : '¥'}</span>{value}{amount === value && <Check size={16} />}</button>)}</div><label className="custom-amount">自定义金额<div><span>{currency === 'usd' ? '$' : '¥'}</span><input type="number" min="1" max="10000" step="0.01" value={amount} onChange={e => setAmount(Number(e.target.value))} /></div></label>{error && <div className="form-error">{error}</div>}<button className="primary-button large" disabled={busy}>{busy ? '正在创建订单…' : <><CreditCard size={18} /> 前往安全支付</>}</button><p className="secure-note"><ShieldCheck size={15} />银行卡信息由 Stripe 加密处理，LLMPay 不保存卡号</p></form>
      <aside className="summary-card"><div className="summary-icon"><WalletCards /></div><h3>本次充值</h3><div className="summary-line"><span>充值金额</span><strong>{money(amount, currency)}</strong></div><div className="summary-line"><span>到账平台余额</span><strong>{amount.toFixed(2)}</strong></div><div className="summary-total"><span>应付总额</span><strong>{money(amount, currency)}</strong></div><ul><li><Check />支付成功后自动到账</li><li><Check />完整订单和余额流水</li><li><Check />如遇问题可凭订单号查询</li></ul></aside></div>
  </div>
}

export function OrdersPage() {
  const [orders, setOrders] = useState<Order[]>([]); const [subscriptions, setSubscriptions] = useState<SubscriptionOrder[]>([]); const [filter, setFilter] = useState('All'); const [loading, setLoading] = useState(true)
  useEffect(() => { Promise.all([api<Order[]>('/api/payments/orders'), api<SubscriptionOrder[]>('/api/mock-recharge/orders')]).then(([payments, rechargeOrders]) => { setOrders(payments); setSubscriptions(rechargeOrders) }).finally(() => setLoading(false)) }, [])
  const shown = filter === 'All' ? orders : orders.filter(x => statusName(x.status) === filter)
  const subscriptionStatus = (value: string) => ({ Created:'待处理', CheckoutCreated:'待支付', PaymentConfirmed:'待验证卡密', CardVerified:'待提交账户', AccountVerified:'待确认账户', AccountConfirmed:'处理中', SubscriptionActivated:'已完成', Cancelled:'已取消', Failed:'失败' }[value] || value)
  const resume = (item: SubscriptionOrder) => { const page = item.status === 'Created' || item.status === 'CheckoutCreated' ? 'payment' : item.status === 'PaymentConfirmed' ? 'verify' : 'recharge'; localStorage.setItem('llmpay_mock_recharge_order', item.id); location.assign(`/app/cards/${page}?id=${item.id}`) }
  return <div className="page"><PageTitle eyebrow="订单中心" title="我的充值订单" text="追踪订阅充值、支付状态和完成时间。" />
    <section className="panel subscription-orders"><div className="panel-head"><div><h3>ChatGPT 订阅订单</h3><p>从付款、账户确认到开通结果的完整记录</p></div><a href="/app/cards">新建订单 <ArrowRight size={15}/></a></div>{subscriptions.length ? <div className="table-wrap"><table><thead><tr><th>订单号</th><th>套餐</th><th>充值账户</th><th>支付方式</th><th>金额</th><th>状态</th><th>创建时间</th><th>操作</th></tr></thead><tbody>{subscriptions.map(item => <tr key={item.id}><td><span className="order-id"><Sparkles size={15}/>{item.orderNumber}</span></td><td>{item.plan === 'Plus' ? 'ChatGPT Plus' : item.plan === 'Pro' ? 'ChatGPT Pro' : 'ChatGPT $100 充值档'}</td><td>{item.targetEmail.startsWith('pending@') ? '待确认' : item.targetEmail}</td><td>{item.paymentProvider === 'wechat' ? '微信支付' : '支付宝'}</td><td>{money(item.amount,'usd')}</td><td><span className={`status ${item.status === 'SubscriptionActivated' ? 'success' : item.status === 'Cancelled' || item.status === 'Failed' ? 'danger' : 'warning'}`}><i/>{subscriptionStatus(item.status)}</span></td><td>{dateTime(item.createdAt)}</td><td><button className="order-open" onClick={() => resume(item)}>{item.status === 'SubscriptionActivated' ? '查看结果' : item.status === 'Cancelled' ? '查看' : '继续处理'}</button></td></tr>)}</tbody></table></div> : <div className="empty-state"><Sparkles/><h4>{loading ? '正在加载…' : '还没有订阅充值订单'}</h4><p>选择 ChatGPT 套餐后，订单会显示在这里。</p></div>}</section>
    <div className="legacy-order-heading"><h3>平台余额订单</h3><p>历史余额充值与支付记录</p></div>
    <div className="filter-tabs">{[['All','全部'],['Pending','待支付'],['Processing','处理中'],['Completed','已完成'],['Failed','失败']].map(([value,label]) => <button className={filter === value ? 'active' : ''} onClick={() => setFilter(value)} key={value}>{label}</button>)}</div>
    <section className="panel"><OrderTable orders={shown} empty={loading ? '正在加载…' : '该分类下没有订单'} /></section>
  </div>
}

function PageTitle({ eyebrow, title, text }: { eyebrow: string; title: string; text: string }) { return <header className="page-title"><span>{eyebrow}</span><h1>{title}</h1><p>{text}</p></header> }
function Metric({ icon: Icon, label, value, hint }: { icon: typeof Sparkles; label: string; value: string; hint: string }) { return <div className="metric-card"><div className="metric-icon"><Icon /></div><div><span>{label}</span><strong>{value}</strong><small>{hint}</small></div></div> }
function OrderTable({ orders, empty }: { orders: Order[]; empty: string }) { return orders.length ? <div className="table-wrap"><table><thead><tr><th>订单号</th><th>充值金额</th><th>到账余额</th><th>状态</th><th>创建时间</th></tr></thead><tbody>{orders.map(order => <tr key={order.orderNumber}><td><span className="order-id"><History size={15} />{order.orderNumber}</span></td><td>{money(order.amount, order.currency)}</td><td>{order.credits.toFixed(2)}</td><td><StatusBadge value={statusName(order.status)} /></td><td>{dateTime(order.createdAt)}</td></tr>)}</tbody></table></div> : <div className="empty-state"><CircleDollarSign /><h4>{empty}</h4><p>充值后，你可以在这里跟踪订单状态。</p></div> }
