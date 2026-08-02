# LLMPay

LLMPay 是一个基于 ASP.NET Core 8、React、PostgreSQL 和 Stripe 的平台余额充值系统。用户给 **LLMPay 平台余额**充值，随后平台可用服务端持有的 OpenAI API Key 调用官方 API 并按用量扣减平台余额。

> 平台余额不是 OpenAI 官方账户余额。本项目不接收 ChatGPT Session Token，不模拟登录，也不调用 ChatGPT 私有支付接口。

## 已实现

- 用户注册、BCrypt 密码哈希和 JWT 登录
- 仅允许登录用户为自己的账户创建充值订单
- Stripe 官方 Checkout Session
- Stripe Webhook HMAC-SHA256 验签及 5 分钟重放窗口
- PostgreSQL 事务入账与充值幂等
- 不可变余额流水和用户订单查询
- EF Core 初始迁移与基础单元测试
- React 响应式用户中心：注册登录、账户概览、充值和订单
- 管理后台：经营概览、全量订单查询/取消、用户查询/停用
- 余额卡购买/独立兑换、支付订单、中断恢复与一次性核销
- 微信支付/支付宝支付配置边界（开发环境使用模拟回调）
- 开发环境 Mock 订阅充值链路：订单、Checkout、支付、伪造账号验证、订阅激活

## 配置

生产环境通过环境变量或密钥管理服务配置，禁止提交真实密钥：

```powershell
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5432;Database=llmpay;Username=postgres;Password=...'
$env:Jwt__SecretKey = '<至少 32 字节的随机密钥>'
$env:Stripe__SecretKey = 'sk_...'
$env:Stripe__WebhookSecret = 'whsec_...'
$env:PublicBaseUrl = 'https://api.example.com'
$env:OpenAI__ApiKey = 'sk-...'
$env:Admin__BootstrapEmail = 'admin@example.com'
$env:Admin__BootstrapPassword = '<本地首次管理员密码>'
$env:Payments__WeChat__MerchantId = '<微信支付商户号>'
$env:Payments__Alipay__AppId = '<支付宝应用 ID>'
```

OpenAI API Key 只能由服务端读取，不能发送给浏览器。官方集成应使用 OpenAI API；新应用优先采用 Responses API：

- [OpenAI 生产最佳实践](https://developers.openai.com/api/docs/guides/production-best-practices)
- [迁移到 Responses API](https://developers.openai.com/api/docs/guides/responses-vs-chat-completions)

## 启动

```powershell
dotnet tool restore
dotnet restore
dotnet ef database update --project LLMPay.Infrastructure --startup-project LLMPay.Api
dotnet run --project LLMPay.Api

cd Frontend
npm install
npm run dev
```

开发环境 Swagger 地址以启动日志为准，通常为 `/swagger`。
前端开发地址默认为 `http://127.0.0.1:5173`，Vite 会将 `/api` 代理到 `http://127.0.0.1:5148`。

## 主要接口

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/payments/recharge-checkout`（Bearer JWT）
- `GET /api/payments/orders`（Bearer JWT）
- `GET /api/payments/wallet`（Bearer JWT）
- `POST /api/payments/webhook/stripe`（Stripe 签名）
- `GET /api/admin/overview`（管理员）
- `GET /api/admin/orders`（管理员）
- `GET /api/admin/users`（管理员）
- `POST /api/card-purchases`（创建余额卡购买订单）
- `POST /api/cards/redeem`（独立核销卡密）
- `GET /api/admin/cards`（管理员卡密管理）
- `POST /api/mock-recharge/orders`（仅开发环境）
- `POST /api/mock-recharge/orders/{id}/verify`（只接受 `test_session_` 伪造 Token）

## Mock 充值测试

`/app/cards` 当前为严格隔离的开发测试页。它不会连接 ChatGPT、OpenAI 或 Stripe，只生成 `cs_test_mock_*` 和 `sub_test_mock_*` 数据。服务端拒绝 JWT、Bearer Token 和非 `test_session_` 格式凭据；Mock Token 不写入数据库、订单或浏览器存储。相关 API 在非 Development 环境返回 404。

OpenAI 请求代理和按模型/token 计费尚未实现；上线前还应加入限流、邮箱验证、退款流程、价格版本表和集成测试。
