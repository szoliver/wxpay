using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.TenPayLibV3;
using System.Xml.Linq;
using Senparc.Weixin.MP;
using System.Web;
using System;

namespace wxPay.Net
{
    public class WxPayV3
    {
        private static TenPayV3Info TenpayV3Info = null;

        public static TenPayV3Info wxPayV3Info
        {
            get { return WxPayV3.TenpayV3Info; }
            set { WxPayV3.TenpayV3Info = value; }
        }
        

        /// <summary>
        /// 签名后执行的委托方法，用于在签名成功的时机有机会处理订单操作
        /// </summary>
        /// <param name="wm">签名成功后返回的相关数据</param>
        /// <param name="openid">签名方法内传出的Openid</param>
        /// <param name="tfee">支付金额</param>
        /// <param name="body">备注信息，原样带入支付流程</param>
        /// <param name="pid">产品ID或是GUID形式的ID</param>
        /// <param name="param">附加数据，用于传递额外的数据以方便处理订单，不带入支付流程也不参与签名，仅用于委托方法处理时使用</param>
        /// <param name="sp_billno">订单号码</param>
        //public delegate void PayOrder(WXPayModel wm, string openid, string tfee, string body, string pid, string param, string sp_billno);
        public delegate void PayOrder(WXPayModel wm);
        
        /// <summary>
        /// 获取微信支付签名信息
        /// 调用前请先配置wxPayV3Info属性，否则会支付失败,如果paySign = "ERROR"，请查看package内容信息
        /// 目前提供最基本的签名字段，有时间的话可以增加基础以外字段的动态增加，一般情况下，基础的字段也能满足支付需求了
        /// 微信支付步骤：</summary>
        /// 1.$.post调用GetWXPayInfo()获取WXPayModel数据，其中包含签名数据<param name="payorder"></param>
        /// 2.wxpay.js调用 WeixinJSBridge.invoke('getBrandWCPayRequest')发起微信支付<param name="openid"></param>
        /// 3.处理回调，成功后返回success
        /// <param name="tfee">支付的金额</param>
        /// <param name="body">备注</param>
        /// <param name="pid">产品信息</param>
        /// <param name="param">附加数据</param>
        /// <param name="sp_billno">订单号码</param>
        /// <returns>返回签名数据，Response给JS呼出微信支付控件，返回WXPayModel.paySign=ERROR时报错</returns>
        public static WXPayModel GetWXPayInfo(PayOrder payorder, string openid, string tfee, string body, string pid, string param, string sp_billno)
        {
            try
            {                
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
                packageReqHandler.SetParameter("appid", wxPayV3Info.AppId);		  //公众账号ID
                packageReqHandler.SetParameter("mch_id", wxPayV3Info.MchId);		  //商户号
                packageReqHandler.SetParameter("nonce_str", nonceStr);                    //随机字符串
                packageReqHandler.SetParameter("body", body);
                packageReqHandler.SetParameter("out_trade_no", sp_billno);		//商家订单号
                packageReqHandler.SetParameter("spbill_create_ip", HttpContext.Current.Request.UserHostAddress);
                packageReqHandler.SetParameter("total_fee", tfee);
                packageReqHandler.SetParameter("notify_url", wxPayV3Info.TenPayV3Notify);		    //接收财付通通知的URL
                packageReqHandler.SetParameter("trade_type", TenPayV3Type.JSAPI.ToString());	 //交易类型
                packageReqHandler.SetParameter("openid", openid);//OPENID

                //获取package包
                string sign = packageReqHandler.CreateMd5Sign("key", wxPayV3Info.Key);
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
                paySignReqHandler.SetParameter("appId", wxPayV3Info.AppId);
                paySignReqHandler.SetParameter("timeStamp", timeStamp);
                paySignReqHandler.SetParameter("nonceStr", nonceStr);
                paySignReqHandler.SetParameter("package", package);
                paySignReqHandler.SetParameter("signType", "MD5");
                paySign = paySignReqHandler.CreateMd5Sign("key", wxPayV3Info.Key);
                WXPayModel wm = new WXPayModel()
                {
                    appId = wxPayV3Info.AppId,
                    nonceStr = nonceStr,
                    package = package,
                    payNo = sp_billno,
                    paySign = paySign,
                    timeStamp = timeStamp
                };
                //payorder(wm, openid, tfee, body, pid, param, sp_billno);
                payorder(wm);
                return wm;

            }
            catch(Exception ex)
            {
                return new WXPayModel() { paySign = "ERROR", package = "抛出异常:"+ ex.Message};
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
