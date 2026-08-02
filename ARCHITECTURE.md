# Architecture

依赖方向：`Api -> Core -> Domain`，`Infrastructure -> Core + Domain`。

- `Domain`：用户、充值订单、余额流水。
- `Core`：注册登录业务、充值订单业务以及支付网关抽象。
- `Infrastructure`：EF Core、PostgreSQL 仓储与事务入账。
- `Api`：JWT、Stripe Checkout/Webhook 和 HTTP 接口。

充值状态流：`Pending -> Processing -> Completed | Failed`。只有通过签名验证的 `checkout.session.completed` Webhook 可以完成入账。`CreditTransaction.ReferenceId` 唯一，并且订单完成状态在同一数据库事务内写入，用于抵御重复 Webhook。

OpenAI API 调用层应在后续独立实现：服务端密钥调用官方 Responses API，读取实际 usage，根据版本化价格表生成负数余额流水。不得接收或存储 ChatGPT Session Token。
