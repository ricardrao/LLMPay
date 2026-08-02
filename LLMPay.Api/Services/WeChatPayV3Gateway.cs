using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using LLMPay.Core.Services;

namespace LLMPay.Api.Services;

public sealed class WeChatPayV3Gateway : INativePaymentGateway
{
    private readonly HttpClient _http; private readonly IConfiguration _config;
    public string Provider=>"wechat";
    public bool IsConfigured=>!string.IsNullOrWhiteSpace(_config["Payments:WeChat:MerchantId"])&&!string.IsNullOrWhiteSpace(_config["Payments:WeChat:AppId"])&&!string.IsNullOrWhiteSpace(_config["Payments:WeChat:CertificateSerialNo"])&&File.Exists(_config["Payments:WeChat:PrivateKeyPath"])&&File.Exists(_config["Payments:WeChat:PlatformCertificatePath"]);
    public WeChatPayV3Gateway(HttpClient http,IConfiguration config){_http=http;_config=config;}

    public async Task<NativePaymentResult> CreateAsync(NativePaymentRequest request,CancellationToken cancellationToken=default)
    {
        if(!IsConfigured)return new(false,null,null,"wechat-v3","微信支付 API v3 尚未配置商户号、证书序列号与密钥");
        const string path="/v3/pay/transactions/native";
        var body=JsonSerializer.Serialize(new{appid=_config["Payments:WeChat:AppId"],mchid=_config["Payments:WeChat:MerchantId"],description=request.Description,out_trade_no=request.OrderNumber,notify_url=request.NotifyUrl,amount=new{total=(int)Math.Round(request.AmountCny*100),currency="CNY"}});
        var message=new HttpRequestMessage(HttpMethod.Post,path){Content=new StringContent(body,Encoding.UTF8,"application/json")};
        message.Headers.Authorization=AuthenticationHeaderValue.Parse(BuildAuthorization("POST",path,body));
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response=await _http.SendAsync(message,cancellationToken);var json=await response.Content.ReadAsStringAsync(cancellationToken);
        if(!response.IsSuccessStatusCode)return new(false,null,null,"wechat-v3",$"微信支付下单失败 ({(int)response.StatusCode}): {json}");
        using var doc=JsonDocument.Parse(json);return new(true,doc.RootElement.GetProperty("code_url").GetString(),request.OrderNumber,"wechat-v3",null);
    }

    public Task<PaymentNotificationResult> VerifyNotificationAsync(string body,IReadOnlyDictionary<string,string> headers,IReadOnlyDictionary<string,string> form,CancellationToken cancellationToken=default)
    {
        try{
            var timestamp=headers["Wechatpay-Timestamp"];var nonce=headers["Wechatpay-Nonce"];var signature=headers["Wechatpay-Signature"];
            if(!long.TryParse(timestamp,out var unix)||Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds()-unix)>300)return Task.FromResult(new PaymentNotificationResult(false,false,null,null,null,"微信回调时间戳已过期"));
            var signed=$"{timestamp}\n{nonce}\n{body}\n";
            using var cert=new System.Security.Cryptography.X509Certificates.X509Certificate2(_config["Payments:WeChat:PlatformCertificatePath"]!);
            if(headers.TryGetValue("Wechatpay-Serial",out var serial)&&!string.Equals(cert.SerialNumber,serial,StringComparison.OrdinalIgnoreCase))return Task.FromResult(new PaymentNotificationResult(false,false,null,null,null,"微信平台证书序列号不匹配"));
            using var rsa=cert.GetRSAPublicKey();
            if(rsa is null||!rsa.VerifyData(Encoding.UTF8.GetBytes(signed),Convert.FromBase64String(signature),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1))return Task.FromResult(new PaymentNotificationResult(false,false,null,null,null,"微信回调验签失败"));
            using var doc=JsonDocument.Parse(body);var resource=doc.RootElement.GetProperty("resource");var plaintext=Decrypt(resource.GetProperty("ciphertext").GetString()!,resource.GetProperty("nonce").GetString()!,resource.TryGetProperty("associated_data",out var ad)?ad.GetString()??"":"");
            using var transaction=JsonDocument.Parse(plaintext);var root=transaction.RootElement;var orderNo=root.GetProperty("out_trade_no").GetString();var tradeNo=root.GetProperty("transaction_id").GetString();var paid=root.GetProperty("trade_state").GetString()=="SUCCESS";var cents=root.GetProperty("amount").GetProperty("total").GetInt32();
            return Task.FromResult(new PaymentNotificationResult(true,paid,orderNo,tradeNo,cents/100m,null));
        }catch(Exception ex){return Task.FromResult(new PaymentNotificationResult(false,false,null,null,null,$"微信回调格式无效: {ex.Message}"));}
    }

    private string BuildAuthorization(string method,string path,string body){var nonce=Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();var timestamp=DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();var message=$"{method}\n{path}\n{timestamp}\n{nonce}\n{body}\n";using var rsa=RSA.Create();rsa.ImportFromPem(File.ReadAllText(_config["Payments:WeChat:PrivateKeyPath"]!));var signature=Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(message),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1));return $"WECHATPAY2-SHA256-RSA2048 mchid=\"{_config["Payments:WeChat:MerchantId"]}\",nonce_str=\"{nonce}\",timestamp=\"{timestamp}\",serial_no=\"{_config["Payments:WeChat:CertificateSerialNo"]}\",signature=\"{signature}\"";}
    private string Decrypt(string ciphertext,string nonce,string associated){var bytes=Convert.FromBase64String(ciphertext);var cipher=bytes[..^16];var tag=bytes[^16..];var plain=new byte[cipher.Length];using var aes=new AesGcm(Encoding.UTF8.GetBytes(_config["Payments:WeChat:ApiV3Key"]!),16);aes.Decrypt(Encoding.UTF8.GetBytes(nonce),cipher,tag,plain,Encoding.UTF8.GetBytes(associated));return Encoding.UTF8.GetString(plain);}
}
