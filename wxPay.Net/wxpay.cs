using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.TenPayLibV3;
using System.Xml.Linq;
using Senparc.Weixin.MP;
using System.Web;
using System;
using System.Text;
using System.Collections.Generic;

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

        public static string GetSignRequest(Dictionary<string, string> parameters)
        {
            IDictionary<string, string> sortedParams = new SortedDictionary<string, string>(parameters);
            IEnumerator<KeyValuePair<string, string>> dem = sortedParams.GetEnumerator();

            StringBuilder query = new StringBuilder();
            while (dem.MoveNext())
            {
                string key = dem.Current.Key;
                string value = dem.Current.Value;
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    query.Append(key).Append("=").Append(value).Append("&");
                }
            }
            string requstString = query.ToString();
            if (requstString.Contains("&"))
            {
                requstString = requstString.Remove(requstString.Length - 1);
            }

            var sha1 = System.Security.Cryptography.SHA1.Create();
            var sha1Arr = sha1.ComputeHash(Encoding.UTF8.GetBytes(requstString));
            StringBuilder enText = new StringBuilder();
            foreach (var b in sha1Arr)
            {
                enText.AppendFormat("{0:x2}", b);
            }
            return enText.ToString();
        }

        /// <summary>
        /// 获取用户收货地址时的addrSign
        /// </summary>
        /// <param name="nonceStr"></param>
        /// <param name="timeStamp"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetUserAddrSign(string appid, string accesstoken, string nonceStr, string timeStamp, string url)
        {
            //string accesstoken = AccessTokenContainer.TryGetAccessToken(dfConfig.AppID, dfConfig.AppSecret);
            Dictionary<string, string> paramlist = new Dictionary<string, string>();
            paramlist.Add("appid", appid);
            paramlist.Add("noncestr", nonceStr);
            paramlist.Add("accesstoken", accesstoken);
            paramlist.Add("timestamp", timeStamp);
            paramlist.Add("url", url);
            return GetSignRequest(paramlist);
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
        public delegate void NotifySuccess(NotyfyResult e);
        public delegate void NotifyFail(NotyfyResult e);

        /// <summary>
        /// 获取微信支付签名信息
        /// 调用前请先配置wxPayV3Info属性，否则会支付失败,如果paySign = "ERROR"，请查看package内容信息
        /// 目前提供最基本的签名字段，有时间的话可以增加基础以外字段的动态增加，一般情况下，基础的字段也能满足支付需求了
        /// 微信支付步骤：</summary>
        /// 1.$.post调用GetWXPayInfo()获取WXPayModel数据，其中包含签名数据
        /// 2.wxpay.js调用 WeixinJSBridge.invoke('getBrandWCPayRequest')发起微信支付
        /// 3.处理回调，成功后返回success
        /// <param name="payorder"></param>
        /// <param name="openid"></param>
        /// <param name="tfee">支付的金额</param>
        /// <param name="body">备注</param>
        /// <param name="pid">产品信息</param>
        /// <param name="sp_billno">订单号码</param>
        /// <returns>返回签名数据，Response给JS呼出微信支付控件，返回WXPayModel.paySign=ERROR时报错</returns>
        public static WXPayModel GetWXPayInfo(PayOrder payorder, string openid, string tfee, string body, string pid, string sp_billno)
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
                    string err_code = res.Element("xml").Element("return_msg").Value;
                    return new WXPayModel() { paySign = "ERROR", package = err_code };
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
            catch (Exception ex)
            {
                return new WXPayModel() { paySign = "ERROR", package = "抛出异常:" + ex.Message };
            }
        }

        /// <summary>
        /// 回调处理
        /// </summary>
        /// <param name="wxKey">微信支付授权KEY</param>
        /// <param name="success"></param>
        /// <param name="fail"></param>
        /// <returns></returns>
        public static string ProcessNotify(string wxKey, NotifySuccess success, NotifyFail fail)
        {
            NotyfyResult result = new NotyfyResult();
            ResponseHandler resHandler = new ResponseHandler(null);
            try
            {
                result.Content = resHandler.ParseXML();
                string openid = resHandler.GetParameter("openid");
                string out_trade_no = resHandler.GetParameter("out_trade_no");
                string transaction_id = resHandler.GetParameter("transaction_id");
                string total_fee = resHandler.GetParameter("total_fee");
                result = new NotyfyResult()
                {
                    Content = resHandler.ParseXML(),
                    out_trade_no = out_trade_no,
                    openid = openid,
                    transaction_id = transaction_id,
                    total_fee = total_fee,
                    appid = resHandler.GetParameter("appid"),
                    fee_type = resHandler.GetParameter("fee_type"),
                    is_subscribe = resHandler.GetParameter("is_subscribe"),
                    mch_id = resHandler.GetParameter("mch_id"),
                    result_code = resHandler.GetParameter("result_code"),
                    time_end = resHandler.GetParameter("time_end"),
                };
                resHandler.SetKey(wxKey);
                bool signResult = resHandler.IsTenpaySign();
                if (signResult)
                {
                    success(result);
                    return "success";
                }
                else
                {
                    fail(result);
                    return "error";
                }
            }
            catch (Exception ex)
            {
                result.Content = ex.Message;
                fail(result);
                return "error";
            }

        }

        public class NotyfyResult
        {
            /// <summary>
            /// 完成的回调返回内容
            /// </summary>
            public string Content { set; get; }
            public string openid { set; get; }
            public string transaction_id { set; get; }
            public string total_fee { set; get; }
            public string appid { set; get; }
            public string fee_type { set; get; }
            public string is_subscribe { set; get; }
            public string mch_id { set; get; }
            public string out_trade_no { set; get; }
            public string result_code { set; get; }
            public string time_end { set; get; }
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
