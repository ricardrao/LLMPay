using LLMPay.Core.Services;
using LLMPay.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMPay.Api.Controllers;

[ApiController,AllowAnonymous,Route("api/payment-notify")]
public class PaymentNotifyController : ControllerBase
{
    private readonly IReadOnlyDictionary<string,INativePaymentGateway> _gateways;
    private readonly MockRechargeService _orders;
    public PaymentNotifyController(IEnumerable<INativePaymentGateway> gateways,MockRechargeService orders){_gateways=gateways.ToDictionary(x=>x.Provider,StringComparer.OrdinalIgnoreCase);_orders=orders;}

    [HttpPost("alipay")]
    public async Task<IActionResult> Alipay(CancellationToken cancellationToken)
    {
        var form=(await Request.ReadFormAsync(cancellationToken)).ToDictionary(x=>x.Key,x=>x.Value.ToString());
        var result=await _gateways["alipay"].VerifyNotificationAsync(string.Empty,Headers(),form,cancellationToken);
        if(!result.Valid||!result.Paid||string.IsNullOrWhiteSpace(result.OrderNumber)||string.IsNullOrWhiteSpace(result.GatewayTradeNo))return Content("failure","text/plain");
        return Content(await _orders.ConfirmGatewayPaymentAsync(result.OrderNumber,result.GatewayTradeNo,result.AmountCny)?"success":"failure","text/plain");
    }

    [HttpPost("wechat")]
    public async Task<IActionResult> WeChat(CancellationToken cancellationToken)
    {
        using var reader=new StreamReader(Request.Body);var body=await reader.ReadToEndAsync(cancellationToken);
        var result=await _gateways["wechat"].VerifyNotificationAsync(body,Headers(),new Dictionary<string,string>(),cancellationToken);
        if(!result.Valid||!result.Paid||string.IsNullOrWhiteSpace(result.OrderNumber)||string.IsNullOrWhiteSpace(result.GatewayTradeNo)||!await _orders.ConfirmGatewayPaymentAsync(result.OrderNumber,result.GatewayTradeNo,result.AmountCny))return StatusCode(400,new{code="FAIL",message=result.Error??"订单校验失败"});
        return Ok(new{code="SUCCESS",message="成功"});
    }

    [HttpGet("alipay-return")]
    public async Task<IActionResult> AlipayReturn([FromQuery(Name="out_trade_no")] string? orderNumber)
    {
        if(string.IsNullOrWhiteSpace(orderNumber))return Redirect("http://127.0.0.1:5173/app/cards");
        var id=await _orders.FindIdByOrderNumberAsync(orderNumber);
        return Redirect(id.HasValue?$"http://127.0.0.1:5173/app/cards/payment?id={id}&returned=1":"http://127.0.0.1:5173/app/cards");
    }
    private Dictionary<string,string> Headers()=>Request.Headers.ToDictionary(x=>x.Key,x=>x.Value.ToString(),StringComparer.OrdinalIgnoreCase);
}
