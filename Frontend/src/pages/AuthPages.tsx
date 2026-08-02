import { ArrowRight, CheckCircle2, CircleDollarSign, Eye, EyeOff, ShieldCheck, Sparkles } from 'lucide-react'
import { FormEvent, useState } from 'react'
import { useAuth } from '../auth/AuthContext'

function AuthFrame({ children, title, subtitle }: { children: React.ReactNode; title: string; subtitle: string }) {
  return <div className="auth-page">
    <section className="auth-story">
      <div className="brand light"><span className="brand-mark"><CircleDollarSign size={22} /></span><span>LLM<b>Pay</b></span></div>
      <div className="story-copy"><span className="eyebrow"><Sparkles size={14} /> AI 服务余额管理平台</span><h1>充值清晰透明，<br />每笔消费都有迹可循。</h1><p>一个账户管理余额、充值订单和 API 消费，让 AI 服务使用更简单。</p>
        <div className="trust-row"><span><ShieldCheck />支付信息由 Stripe 安全处理</span><span><CheckCircle2 />余额流水实时可查</span></div>
      </div>
      <p className="auth-note">平台余额不等同于 OpenAI 官方账户余额</p>
    </section>
    <section className="auth-panel"><div className="auth-card"><div className="auth-heading"><h2>{title}</h2><p>{subtitle}</p></div>{children}</div></section>
  </div>
}

function PasswordInput({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  const [visible, setVisible] = useState(false)
  return <div className="password-field"><input type={visible ? 'text' : 'password'} value={value} onChange={e => onChange(e.target.value)} placeholder="至少 8 位字符" required minLength={8} /><button type="button" onClick={() => setVisible(!visible)}>{visible ? <EyeOff size={18} /> : <Eye size={18} />}</button></div>
}

export function LoginPage() {
  const { user, login } = useAuth()
  const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false)
  if (user) { location.replace(user.role === 'Admin' ? '/admin' : '/app'); return null }
  const submit = async (e: FormEvent) => { e.preventDefault(); setError(''); setBusy(true); try { const result = await login(email, password); location.assign(result.role === 'Admin' ? '/admin' : '/app') } catch (ex) { setError((ex as Error).message) } finally { setBusy(false) } }
  return <AuthFrame title="欢迎回来" subtitle="登录后继续管理你的余额和订单">
    <form onSubmit={submit} className="form-stack"><label>邮箱地址<input type="email" placeholder="name@example.com" value={email} onChange={e => setEmail(e.target.value)} required /></label><label>登录密码<PasswordInput value={password} onChange={setPassword} /></label>{error && <div className="form-error">{error}</div>}<button className="primary-button" disabled={busy}>{busy ? '正在登录…' : <>登录账户 <ArrowRight size={17} /></>}</button></form>
    <p className="auth-switch">还没有账户？<a href="/register">免费注册</a></p>
  </AuthFrame>
}

export function RegisterPage() {
  const { user, register, login } = useAuth()
  const [form, setForm] = useState({ nickname: '', email: '', password: '' }); const [error, setError] = useState(''); const [busy, setBusy] = useState(false)
  if (user) { location.replace('/app'); return null }
  const submit = async (e: FormEvent) => { e.preventDefault(); setError(''); setBusy(true); try { await register(form.email, form.password, form.nickname); await login(form.email, form.password); location.assign('/app') } catch (ex) { setError((ex as Error).message) } finally { setBusy(false) } }
  return <AuthFrame title="创建账户" subtitle="一分钟开始使用 LLMPay">
    <form onSubmit={submit} className="form-stack"><label>称呼<input placeholder="例如：小林" value={form.nickname} onChange={e => setForm({ ...form, nickname: e.target.value })} maxLength={100} /></label><label>邮箱地址<input type="email" placeholder="name@example.com" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} required /></label><label>设置密码<PasswordInput value={form.password} onChange={password => setForm({ ...form, password })} /></label>{error && <div className="form-error">{error}</div>}<button className="primary-button" disabled={busy}>{busy ? '正在创建…' : <>创建账户 <ArrowRight size={17} /></>}</button></form>
    <p className="terms">注册即表示你同意平台服务条款和隐私政策。</p><p className="auth-switch">已有账户？<a href="/login">直接登录</a></p>
  </AuthFrame>
}
