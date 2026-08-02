using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LLMPay.Core.Services;

namespace LLMPay.Api.Services;

public sealed class AlipaySandboxGateway : INativePaymentGateway
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    public string Provider => "alipay";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config["Payments:Alipay:AppId"])
        && File.Exists(_config["Payments:Alipay:PrivateKeyPath"])
        && File.Exists(_config["Payments:Alipay:AlipayPublicKeyPath"]);

    public AlipaySandboxGateway(HttpClient http, IConfiguration config) { _http=http; _config=config; }

    public async Task<NativePaymentResult> CreateAsync(NativePaymentRequest request, CancellationToken cancellationToken=default)
    {
        if (!IsConfigured) return new(false,null,null,"alipay-sandbox","支付宝沙箱尚未配置 AppId 与 RSA2 密钥");
        var parameters=new SortedDictionary<string,string>(StringComparer.Ordinal)
        {
            ["app_id"]=_config["Payments:Alipay:AppId"]!, ["method"]="alipay.trade.page.pay", ["format"]="JSON",
            ["charset"]="utf-8", ["sign_type"]="RSA2", ["timestamp"]=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ["version"]="1.0",
            ["notify_url"]=request.NotifyUrl,
            ["return_url"]=(_config["PublicBaseUrl"]??string.Empty).TrimEnd('/')+"/api/payment-notify/alipay-return",
            ["biz_content"]=JsonSerializer.Serialize(new { out_trade_no=request.OrderNumber, total_amount=request.AmountCny.ToString("0.00",CultureInfo.InvariantCulture), subject=request.Description, product_code="FAST_INSTANT_TRADE_PAY", timeout_express="15m" })
        };
        parameters["sign"]=Sign(Canonical(parameters),File.ReadAllText(_config["Payments:Alipay:PrivateKeyPath"]!));
        var gateway=_config["Payments:Alipay:Gateway"]??"https://openapi-sandbox.dl.alipaydev.com/gateway.do";
        var query=string.Join("&",parameters.Select(x=>$"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        return await Task.FromResult(new NativePaymentResult(true,$"{gateway}?{query}",request.OrderNumber,"alipay-page-sandbox",null));
    }

    public Task<PaymentNotificationResult> VerifyNotificationAsync(string body,IReadOnlyDictionary<string,string> headers,IReadOnlyDictionary<string,string> form,CancellationToken cancellationToken=default)
    {
        if(!IsConfigured||!form.TryGetValue("sign",out var signature))return Task.FromResult(new PaymentNotificationResult(false,false,null,null,null,"支付宝回调缺少签名或配置"));
        var signed=new SortedDictionary<string,string>(form.Where(x=>x.Key is not "sign" and not "sign_type").ToDictionary(x=>x.Key,x=>x.Value),StringComparer.Ordinal);
        var valid=Verify(Canonical(signed),signature,File.ReadAllText(_config["Payments:Alipay:AlipayPublicKeyPath"]!));
        form.TryGetValue("out_trade_no",out var orderNo); form.TryGetValue("trade_no",out var tradeNo); form.TryGetValue("trade_status",out var status);
        decimal? amount=form.TryGetValue("total_amount",out var raw)&&decimal.TryParse(raw,NumberStyles.Number,CultureInfo.InvariantCulture,out var parsed)?parsed:null;
        var appMatches=form.TryGetValue("app_id",out var appId)&&appId==_config["Payments:Alipay:AppId"];
        return Task.FromResult(new PaymentNotificationResult(valid&&appMatches,valid&&appMatches&&status is "TRADE_SUCCESS" or "TRADE_FINISHED",orderNo,tradeNo,amount,valid?null:"支付宝回调验签失败"));
    }

    private static string Canonical(IEnumerable<KeyValuePair<string,string>> values)=>string.Join("&",values.Where(x=>!string.IsNullOrEmpty(x.Value)).Select(x=>$"{x.Key}={x.Value}"));
    private static string Sign(string value,string pem){using var rsa=RSA.Create();rsa.ImportFromPem(pem);return Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(value),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1));}
    private static bool Verify(string value,string signature,string pem){try{using var rsa=RSA.Create();rsa.ImportFromPem(pem);return rsa.VerifyData(Encoding.UTF8.GetBytes(value),Convert.FromBase64String(signature),HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);}catch{return false;}}
}
