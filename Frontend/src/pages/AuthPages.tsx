import { ArrowLeft, ArrowRight, CheckCircle2, Eye, EyeOff, ShieldCheck, Sparkles, Zap } from 'lucide-react'
import { FormEvent, useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { api } from '../lib/api'

function AuthFrame({ children, title, subtitle }: { children: React.ReactNode; title: string; subtitle: string }) {
  return <div className="auth-page">
    <header className="auth-nav"><a href="/app/cards"><Zap/><span>LLM<b>Pay</b></span></a><a href="/app/cards"><ArrowLeft/>返回充值首页</a></header>
    <main className="auth-shell"><section className="auth-story">
      <div className="story-copy"><span className="eyebrow"><Sparkles size={14} /> 充值记录，随时可查</span><h1>管理你的订单，<br/><em>不用从头再来。</em></h1><p>登录后可查看历史充值、继续处理中断的订单，并及时确认每一笔结果。</p>
        <div className="trust-row"><span><ShieldCheck />订单信息安全关联</span><span><CheckCircle2 />中断流程随时继续</span></div>
      </div>
      <div className="auth-order-preview"><span>最近订单</span><div><i><Zap/></i><p>ChatGPT Plus<b>充值已完成</b></p><strong>US$20</strong></div></div>
    </section>
    <section className="auth-panel"><div className="auth-card"><div className="auth-heading"><span>ACCOUNT</span><h2>{title}</h2><p>{subtitle}</p></div>{children}<a className="auth-home-link" href="/app/cards">暂不登录，直接充值 <ArrowRight/></a></div></section></main>
  </div>
}

function PasswordInput({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  const [visible, setVisible] = useState(false)
  return <div className="password-field"><input type={visible ? 'text' : 'password'} value={value} onChange={e => onChange(e.target.value)} placeholder="至少 8 位字符" required minLength={8} /><button type="button" onClick={() => setVisible(!visible)}>{visible ? <EyeOff size={18} /> : <Eye size={18} />}</button></div>
}

export function LoginPage() {
  const { user, login } = useAuth()
  const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false)
  if (user) { location.replace(user.role === 'Admin' ? '/admin' : '/app/orders'); return null }
  const submit = async (e: FormEvent) => { e.preventDefault(); setError(''); setBusy(true); try { const result = await login(email, password); location.assign(result.role === 'Admin' ? '/admin' : '/app/orders') } catch (ex) { setError((ex as Error).message) } finally { setBusy(false) } }
  return <AuthFrame title="欢迎回来" subtitle="登录后查看历史充值和继续未完成订单">
    <form onSubmit={submit} className="form-stack"><label>邮箱地址<input type="email" placeholder="name@example.com" value={email} onChange={e => setEmail(e.target.value)} required /></label><label>登录密码<PasswordInput value={password} onChange={setPassword} /></label>{error && <div className="form-error">{error}</div>}<button className="primary-button" disabled={busy}>{busy ? '正在登录…' : <>登录账户 <ArrowRight size={17} /></>}</button></form>
    <p className="auth-switch">还没有账户？<a href="/register">免费注册</a></p>
  </AuthFrame>
}

export function RegisterPage() {
  const { user, register, login } = useAuth()
  const [form, setForm] = useState({ nickname: '', email: '', password: '' }); const [error, setError] = useState(''); const [busy, setBusy] = useState(false)
  if (user) { location.replace(user.role === 'Admin' ? '/admin' : '/app/orders'); return null }
  const submit = async (e: FormEvent) => { e.preventDefault(); setError(''); setBusy(true); try { await register(form.email, form.password, form.nickname); await login(form.email, form.password); location.assign('/app/orders') } catch (ex) { setError((ex as Error).message) } finally { setBusy(false) } }
  return <AuthFrame title="创建账户" subtitle="创建后可统一查看和管理充值订单">
    <form onSubmit={submit} className="form-stack"><label>称呼<input placeholder="例如：小林" value={form.nickname} onChange={e => setForm({ ...form, nickname: e.target.value })} maxLength={100} /></label><label>邮箱地址<input type="email" placeholder="name@example.com" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} required /></label><label>设置密码<PasswordInput value={form.password} onChange={password => setForm({ ...form, password })} /></label>{error && <div className="form-error">{error}</div>}<button className="primary-button" disabled={busy}>{busy ? '正在创建…' : <>创建账户 <ArrowRight size={17} /></>}</button></form>
    <p className="terms">注册即表示你同意平台服务条款和隐私政策。</p><p className="auth-switch">已有账户？<a href="/login">直接登录</a></p>
  </AuthFrame>
}

export function SetPasswordPage() {
  const params=new URLSearchParams(location.search); const email=params.get('email')||''; const token=params.get('token')||''
  const [password,setPassword]=useState(''); const [error,setError]=useState(''); const [done,setDone]=useState(false); const [busy,setBusy]=useState(false)
  const submit=async(e:FormEvent)=>{e.preventDefault();setBusy(true);setError('');try{await api('/api/auth/set-password',{method:'POST',body:JSON.stringify({email,token,password})});setDone(true)}catch(ex){setError((ex as Error).message)}finally{setBusy(false)}}
  return <AuthFrame title="设置登录密码" subtitle={`为 ${email||'你的账户'} 设置密码`} >{done?<div className="empty-state"><CheckCircle2/><h4>密码设置成功</h4><a className="primary-button" href="/login">前往登录</a></div>:<form onSubmit={submit} className="form-stack"><label>新密码<PasswordInput value={password} onChange={setPassword}/></label>{error&&<div className="form-error">{error}</div>}<button className="primary-button" disabled={busy||!email||!token}>{busy?'正在设置…':'设置密码'}</button></form>}</AuthFrame>
}
