using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.TenPayLibV3;
using System.Xml.Linq;
using Senparc.Weixin.MP;
using System.Web;

namespace wxPay.Net
{
    public class WxPayV3
    {
        public static string appID = "";
        public static string appsecret = "";
        public static string MchId = "";
        public static string WxKey = "";
        public static string PayNotifyUrl = "";

        public delegate void PayOrder(WXPayModel wm, string openid, string tfee, string body, string pid, string param, string sp_billno);
        /// <summary>
        /// 获取微信支付信息
        /// 微信支付步骤：
        /// 1.$.post调用GetWXPayInfo()获取WXPayModel数据，其中包含签名数据
        /// 2.wxpay.js调用 WeixinJSBridge.invoke('getBrandWCPayRequest')发起微信支付
        /// 3.处理回调，成功后返回success
        /// </summary>
        /// <returns></returns>
        public static WXPayModel GetWXPayInfo(PayOrder payorder, string openid, string tfee, string body, string pid, string param, string sp_billno)
        {
            try
            {
                TenPayV3Info tpv = new TenPayV3Info(appID, appsecret, MchId, WxKey, PayNotifyUrl);

                string timeStamp = "";
                string nonceStr = "";
                string package = "";
                string paySign = "";

                //创建支付应答对象
                RequestHandler packageReqHandler = new RequestHandler(null);
                //初始化
                packageReqHandler.Init();

                //调起微信支付签名
                timeStamp = TenPayV3Util.GetTimestamp();
                nonceStr = TenPayV3Util.GetNoncestr();

                //设置package订单参数
                packageReqHandler.SetParameter("appid", tpv.AppId);		  //公众账号ID
                packageReqHandler.SetParameter("mch_id", tpv.MchId);		  //商户号
                packageReqHandler.SetParameter("nonce_str", nonceStr);                    //随机字符串
                packageReqHandler.SetParameter("body", body);
                packageReqHandler.SetParameter("out_trade_no", sp_billno);		//商家订单号
                packageReqHandler.SetParameter("spbill_create_ip", HttpContext.Current.Request.UserHostAddress);
                packageReqHandler.SetParameter("total_fee", tfee);
                packageReqHandler.SetParameter("notify_url", tpv.TenPayV3Notify);		    //接收财付通通知的URL
                packageReqHandler.SetParameter("trade_type", TenPayV3Type.JSAPI.ToString());	 //交易类型
                packageReqHandler.SetParameter("openid", openid);//OPENID

                //获取package包
                string sign = packageReqHandler.CreateMd5Sign("key", tpv.Key);
                packageReqHandler.SetParameter("sign", sign);
                string data = packageReqHandler.ParseXML();
                var result = TenPayV3.Unifiedorder(data);
                var res = XDocument.Parse(result);
                if (res.Element("xml").Element("prepay_id") == null)
                {
                    return new WXPayModel() { paySign = "ERROR", package = result };
                }
                string prepayId = res.Element("xml").Element("prepay_id").Value;
                package = string.Format("prepay_id={0}", prepayId);
                //设置支付参数
                RequestHandler paySignReqHandler = new RequestHandler(null);
                paySignReqHandler.SetParameter("appId", tpv.AppId);
                paySignReqHandler.SetParameter("timeStamp", timeStamp);
                paySignReqHandler.SetParameter("nonceStr", nonceStr);
                paySignReqHandler.SetParameter("package", package);
                paySignReqHandler.SetParameter("signType", "MD5");
                paySign = paySignReqHandler.CreateMd5Sign("key", tpv.Key);
                WXPayModel wm = new WXPayModel()
                {
                    appId = tpv.AppId,
                    nonceStr = nonceStr,
                    package = package,
                    payNo = sp_billno,
                    paySign = paySign,
                    timeStamp = timeStamp
                };
                payorder(wm, openid, tfee, body, pid, param, sp_billno);
                return wm;

            }
            catch
            {
                return new WXPayModel() { paySign = "ERROR", package = "抛出异常" };
            }

        }

        public class WXPayModel
        {
            /// <summary>
            /// appId
            /// </summary>
            public string appId { get; set; }

            /// <summary>
            /// timeStamp
            /// </summary>
            public string timeStamp { get; set; }

            /// <summary>
            /// nonceStr
            /// </summary>
            public string nonceStr { get; set; }

            /// <summary>
            /// paySign
            /// </summary>
            public string paySign { get; set; }

            /// <summary>
            /// package
            /// </summary>
            public string package { get; set; }

            /// <summary>
            /// 返回的支付号，可能是订单号，也可能是充值ID
            /// </summary>
            public string payNo { get; set; }


        }
    }
}
