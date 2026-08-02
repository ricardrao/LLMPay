# 支付测试环境配置

密钥不要提交到 `appsettings.json`。本地使用 .NET User Secrets：

```powershell
dotnet user-secrets set "PublicBaseUrl" "https://你的公网HTTPS域名" --project LLMPay.Api
dotnet user-secrets set "Payments:UseGatewaySandbox" "true" --project LLMPay.Api
```

## 支付宝沙箱

准备沙箱应用 AppId、应用 RSA2 私钥 PEM、支付宝沙箱公钥 PEM：

```powershell
dotnet user-secrets set "Payments:Alipay:AppId" "沙箱APPID" --project LLMPay.Api
dotnet user-secrets set "Payments:Alipay:PrivateKeyPath" "C:\安全目录\alipay-app-private.pem" --project LLMPay.Api
dotnet user-secrets set "Payments:Alipay:AlipayPublicKeyPath" "C:\安全目录\alipay-public.pem" --project LLMPay.Api
```

本沙箱文档支持的是 `alipay.trade.page.pay`，不是 `alipay.trade.precreate`。页面会打开支付宝沙箱电脑网站收银台，而不是由接口直接返回二维码。

异步回调地址：`https://你的域名/api/payment-notify/alipay`

## 微信支付 API v3

微信 API v3 没有与支付宝相同的公共沙箱收银台。本项目使用 Native 支付商户联调，测试金额默认 `¥0.01`：

```powershell
dotnet user-secrets set "Payments:WeChat:MerchantId" "商户号" --project LLMPay.Api
dotnet user-secrets set "Payments:WeChat:AppId" "AppId" --project LLMPay.Api
dotnet user-secrets set "Payments:WeChat:ApiV3Key" "32位APIv3密钥" --project LLMPay.Api
dotnet user-secrets set "Payments:WeChat:CertificateSerialNo" "商户证书序列号" --project LLMPay.Api
dotnet user-secrets set "Payments:WeChat:PrivateKeyPath" "C:\安全目录\apiclient_key.pem" --project LLMPay.Api
dotnet user-secrets set "Payments:WeChat:PlatformCertificatePath" "C:\安全目录\wechatpay-platform.pem" --project LLMPay.Api
```

回调地址：`https://你的域名/api/payment-notify/wechat`

支付成功只由验签回调确认。沙箱/商户联调模式下，前端不会显示手动确认付款按钮。
