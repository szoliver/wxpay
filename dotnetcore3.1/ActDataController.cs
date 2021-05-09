using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using AdminCP.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using System.IO;
using Dos.ORM;
using Dos.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Text;
using System.Text.Json;
using ToolGood.Words.Pinyin;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Xml;
using System.Xml.Serialization;
using wxPay.Net;
using Senparc.Weixin.TenPay;
using Senparc.Weixin.TenPay.V3;

namespace AdminCP.Controllers
{
    [Authorize]
    public class ActDataController : Controller
    {
        [HttpPost]
        [AllowAnonymous]
        public JsonResult GetSystemTimeTicks()
        {
            return new JsonResult(new { ticks = DateTime.Now.Ticks / 10000000 });
        }
        [AllowAnonymous]
        public ContentResult WXH5Test()
        {
            return Content("success");
        }

        [HttpPost]
        [AllowAnonymous]
        public ContentResult WXH5Notify()
        {
            //http://aos.nobbus.cn/actdata/wxh5notify
            string key = AOSConfig.WXCFG.wxPayKeyV3;
            ResponseHandler resHandler = new ResponseHandler(null);
            Action<wxPay.Net.WxPayV3.NotifyResult> f = null;
            f = (res) =>
            {
                act_2_PayLog pay = DB.MysqlContext.From<act_2_PayLog>().Where(c => c.logMerchOrder == res.out_trade_no).ToFirst();
                if (pay != null)
                {
                    //通常情况下不可能为空，除非人为从数据库删除
                    pay.logOrderID = res.transaction_id;//WX PAY ORDER ID
                    pay.logPayTime = DateTime.ParseExact(res.time_end, "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
                    pay.logPayStatus = res.result_code;
                    DB.MysqlContext.Update(pay);
                }
            };
            string result = WxPayV3.ProcessNotify(key, resHandler, delegate (wxPay.Net.WxPayV3.NotifyResult res)
             {
                 //Dos.Common.LogHelper.Debug("NotifyUrl Success:" + res.Content, "WXPAY_Notify");
                 //TODO:回调成功时处理
                 //<xml><is_subscribe><![CDATA[Y]]></is_subscribe><total_fee>1</total_fee><transaction_id><![CDATA[4200001023202105105474736768]]></transaction_id>
                 //<return_code><![CDATA[SUCCESS]]></return_code><time_end><![CDATA[20210510034403]]></time_end><cash_fee>1</cash_fee><mch_id><![CDATA[1489412872]]></mch_id>
                 //<openid><![CDATA[o2Spg6CUIpmRIdQ1l_u-sbsJsEjE]]></openid><appid><![CDATA[wxab8a78d26ddb88d9]]></appid><fee_type><![CDATA[CNY]]></fee_type>
                 //<nonce_str><![CDATA[8B67E8D41EDDB083BD3A3470153F88E5]]></nonce_str><sign><![CDATA[BD1EAA4008C22BC6BE4EEAB03FB42083]]></sign>
                 //<trade_type><![CDATA[JSAPI]]></trade_type><out_trade_no><![CDATA[9b1314e5972042fbb16ef26fb2825522]]></out_trade_no>
                 //<bank_type><![CDATA[CMB_CREDIT]]></bank_type><result_code><![CDATA[SUCCESS]]></result_code></xml>
                 f(res);
             }, delegate (wxPay.Net.WxPayV3.NotifyResult res)
             {
                 Dos.Common.LogHelper.Debug("NotifyUrl Fail:" + res.Content, "WXPAY_Notify");
                 //TODO:回调失败时处理
                 f(res);
             });
            //此处一定要返回，不然微信服务器收不到确认信息
            return Content(result);
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult Payment(string openid, int tfee, string body, string pid, PreGroupParam param, string sp_billno)
        {
            string ipv4 = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            if (string.IsNullOrEmpty(sp_billno)) sp_billno = Guid.NewGuid().ToString("N");
            //Dos.Common.LogHelper.Debug($"openid:{openid},tfee:{tfee},body:{body},pid:{pid},param:{System.Text.Json.JsonSerializer.Serialize(param)},sp_billno:{sp_billno}", "WXPAY");
            string payNotify = AOSConfig.WXCFG.wxPayNotify;
            string openNotify = AOSConfig.WXCFG.wxOpenNotify;
            WxPayV3.wxPayV3Info = new TenPayV3Info(
                AOSConfig.WXCFG.wcAppID,
                AOSConfig.WXCFG.wcAppSecret,
                AOSConfig.WXCFG.wxMchID,
                AOSConfig.WXCFG.wxPayKey,
                AOSConfig.WXCFG.wxCertPath,
                AOSConfig.WXCFG.wxCertSecret,
                payNotify,
                openNotify);
            if (sp_billno == "") sp_billno = Guid.NewGuid().ToString("N");
            JsonResult jsonResult = GenOrder(param);
            //Dos.Common.LogHelper.Debug("预备团：" + System.Text.Json.JsonSerializer.Serialize(jsonResult.Value), "WXPAY");
            if (tfee < 0) tfee = Convert.ToInt32(((RetObject)(jsonResult.Value)).Price * 100);
            WxPayV3.WXPayModel model = WxPayV3.GetWXPayInfo(delegate (WxPayV3.WXPayModel wm)
            {
                //签名后获得的WXPayModel结果对象，此时可;以写入订单
                //如果paySign为Error，请检查package内容进行调试和查找错误                
                act_2_PayLog order = new act_2_PayLog()
                {
                    logInst = ((RetObject)jsonResult.Value).Inst,
                    logMerchOrder = sp_billno,
                    logOrderID = wm.payNo,
                    logOrdertime = TimeZoneInfo.ConvertTimeToUtc(new DateTime(1970, 1, 1, 8, 0, 0)).AddSeconds(double.Parse(wm.timeStamp)).ToLocalTime(),
                    logPayAmt = 0, //支付回调时更新实际支付的金额
                    logPayStatus = "PREORDER",
                    logPayTime = DateTime.Now, //支付回调时更新支付时间
                    logPrice = wm.payFee,
                    logApplyID = ((RetObject)jsonResult.Value).Code,
                    logGID = param.gid, //所属团的团长ID，大于0表示是团员参团，等于0表示自己就是团长
                    logWxUid = ((RetObject)jsonResult.Value).WxUID,
                    logAddtime = DateTime.Now
                };
                DB.MysqlContext.Insert(order);
            }, openid, tfee, body, pid, sp_billno, ipv4);
            model.payNo = sp_billno;
            model.mchOrder = sp_billno;
            //Dos.Common.LogHelper.Debug("支付结果：" + System.Text.Json.JsonSerializer.Serialize(model), "WXPAY");
            //return JsonConvert.SerializeObject(model);
            return new JsonResult(model);
        }        
    }
}
