import { ArrowLeft, ArrowRight, CheckCircle2, Clock3, CreditCard, ReceiptText, RefreshCw, Search, ShieldCheck, Sparkles, TimerReset, XCircle, Zap } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { api, dateTime, money } from '../lib/api'

type Order = { id:string; orderNumber:string; targetEmail:string; targetName:string; plan:'Plus'|'Credit100'|'Pro'; paymentProvider:'wechat'|'alipay'; amount:number; status:string; createdAt:string; completedAt?:string }
const planName=(id:Order['plan'])=>id==='Plus'?'ChatGPT Plus':id==='Pro'?'ChatGPT Pro':'ChatGPT $100 充值档'
const state=(value:string)=>({Created:['订单已创建','pending'],CheckoutCreated:['等待支付','pending'],PaymentConfirmed:['等待验证卡密','pending'],CardVerified:['等待提交账户','processing'],AccountVerified:['等待确认账户','processing'],AccountConfirmed:['正在开通','processing'],SubscriptionActivated:['充值成功','success'],Cancelled:['已取消','cancelled'],Failed:['处理失败','cancelled']}[value]||[value,'pending'])
const destination=(order:Order)=>order.status==='Created'||order.status==='CheckoutCreated'?'payment':order.status==='PaymentConfirmed'?'verify':'recharge'

export function SubscriptionOrdersPage(){
  const {user}=useAuth()
  const [orders,setOrders]=useState<Order[]>([])
  const [loading,setLoading]=useState(true)
  const [busyId,setBusyId]=useState('')
  const [query,setQuery]=useState('')
  const [filter,setFilter]=useState('all')
  const load=()=>{setLoading(true);api<Order[]>('/api/mock-recharge/orders').then(setOrders).finally(()=>setLoading(false))}
  useEffect(load,[])
  const shown=useMemo(()=>orders.filter(order=>(filter==='all'||filter==='active'&&!['SubscriptionActivated','Cancelled','Failed'].includes(order.status)||filter==='done'&&order.status==='SubscriptionActivated')&&(!query||order.orderNumber.toLowerCase().includes(query.toLowerCase())||order.targetEmail.toLowerCase().includes(query.toLowerCase()))),[orders,filter,query])
  const resume=(order:Order)=>{localStorage.setItem('llmpay_mock_recharge_order',order.id);location.assign(`/app/cards/${destination(order)}?id=${order.id}`)}
  const cancel=async(order:Order)=>{
    if(!confirm(`确定取消订单 ${order.orderNumber} 吗？取消后不能恢复。`)) return
    setBusyId(order.id)
    try{
      const updated=await api<Order>(`/api/mock-recharge/orders/${order.id}/cancel`,{method:'POST'})
      setOrders(items=>items.map(item=>item.id===updated.id?updated:item))
      if(localStorage.getItem('llmpay_mock_recharge_order')===order.id)localStorage.removeItem('llmpay_mock_recharge_order')
    }catch(ex){alert((ex as Error).message)}finally{setBusyId('')}
  }
  return <div className="order-site">
    <div className="test-environment"><Sparkles/> 测试环境 · 订单数据仅用于本地流程体验</div>
    <header className="recharge-nav"><a href="/app/cards"><Zap/><span>LLM<b>Pay</b></span></a><nav><a href="/app/cards">充值套餐</a><a className="current" href="/app/orders">我的订单</a></nav><span>{user?.nickname||user?.email}</span></header>
    <main className="orders-main">
      <a className="orders-back" href="/app/cards"><ArrowLeft/>返回充值首页</a>
      <section className="orders-hero"><div><span>MY ORDERS</span><h1>我的充值订单</h1><p>查看每笔 ChatGPT 充值的当前进度，或继续完成尚未结束的订单。</p></div><aside><div><span>全部订单</span><strong>{orders.length}</strong></div><div><span>处理中</span><strong>{orders.filter(x=>!['SubscriptionActivated','Cancelled','Failed'].includes(x.status)).length}</strong></div><div><span>已完成</span><strong>{orders.filter(x=>x.status==='SubscriptionActivated').length}</strong></div></aside></section>
      <aside className="paid-order-notice"><ShieldCheck/><div><b>付款后中断也不会丢失</b><span>已生成卡密或已验证卡密的订单会保留当前进度，点击“继续处理”即可从中断位置恢复。</span></div></aside>
      <section className="orders-toolbar"><div className="order-tabs">{[['all','全部订单'],['active','进行中'],['done','已完成']].map(([value,label])=><button className={filter===value?'active':''} onClick={()=>setFilter(value)} key={value}>{label}</button>)}</div><label><Search/><input value={query} onChange={e=>setQuery(e.target.value)} placeholder="搜索订单号或充值邮箱"/></label><button className="refresh-orders" onClick={load}><RefreshCw/>刷新</button></section>
      <section className="order-list">{shown.map(order=>{
        const [label,tone]=state(order.status)
        const canCancel=['Created','CheckoutCreated'].includes(order.status)
        return <article className={`subscription-order-card ${tone}`} key={order.id}>
          <div className="order-card-head"><div className="order-product-icon">{order.status==='SubscriptionActivated'?<CheckCircle2/>:<ReceiptText/>}</div><div><span>{planName(order.plan)}</span><b>{order.orderNumber}</b></div><em className={tone}><i/>{label}</em></div>
          <div className="order-card-body"><dl><div><dt>充值账户</dt><dd>{order.targetEmail.startsWith('pending@')?'等待确认':order.targetEmail}</dd></div><div><dt>支付方式</dt><dd>{order.paymentProvider==='wechat'?'微信支付':'支付宝'}</dd></div><div><dt>下单时间</dt><dd>{dateTime(order.createdAt)}</dd></div><div><dt>{order.completedAt?'完成时间':'订单金额'}</dt><dd>{order.completedAt?dateTime(order.completedAt):money(order.amount,'usd')}</dd></div></dl><strong>{money(order.amount,'usd')}</strong></div>
          <footer><p>{order.status==='SubscriptionActivated'?<><CheckCircle2/>该订单已完成，可以查看充值结果</>:order.status==='Cancelled'?<><TimerReset/>该订单已取消</>:canCancel?<><Clock3/>尚未付款，可继续支付或取消订单</>:<><Clock3/>付款已确认，进度已保留，可继续完成充值</>}</p><div className="order-actions">{canCancel&&<button className="cancel-order" disabled={busyId===order.id} onClick={()=>void cancel(order)}><XCircle/>{busyId===order.id?'正在取消':'取消订单'}</button>}{order.status!=='Cancelled'&&<button onClick={()=>resume(order)}>{order.status==='SubscriptionActivated'?'查看结果':'继续处理'}<ArrowRight/></button>}</div></footer>
        </article>
      })}{!shown.length&&<div className="orders-empty"><div><ReceiptText/></div><h2>{loading?'正在加载订单…':'这里还没有订单'}</h2><p>{query?'没有找到匹配的订单':'选择一个 ChatGPT 套餐，完成你的第一笔充值体验。'}</p>{!loading&&!query&&<a href="/app/cards"><CreditCard/>选择充值套餐</a>}</div>}</section>
    </main>
    <footer className="recharge-footer"><a href="/app/cards"><Zap/> LLMPay</a><p>本地完整流程测试环境</p><nav><a href="/app/cards">充值套餐</a><a href="/app/orders">我的订单</a></nav></footer>
  </div>
}
