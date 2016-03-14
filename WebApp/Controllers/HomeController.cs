using Newtonsoft.Json;
using Senparc.Weixin.MP.AdvancedAPIs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using wxPay.Net;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(string code = "")
        {
            //取accesstoken，非组件中的var accessToken = AccessTokenContainer.TryGetAccessToken(appId, appSecret);，切记切记！！
            //本例中进行了授权跳转，取到code才能取到OAuth2的AccessToken
            string appid = "";
            string RootDomain = "";
            if (code == "")
            {
                Response.Redirect(OAuthApi.GetAuthorizeUrl(appid, RootDomain + "home/index", "dfwxmall", Senparc.Weixin.MP.OAuthScope.snsapi_userinfo));
                Response.End();
            }
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "";

            return View();

        }

        [HttpPost]
        public string Payment(string openid, string tfee, string body, string pid, string param, string sp_billno)
        {
            WxPayV3.wxPayV3Info = new Senparc.Weixin.MP.TenPayLibV3.TenPayV3Info("appid", "appsecert", "mchid", "key", "http://localhost:63014/home/success");
            WxPayV3.WXPayModel model = WxPayV3.GetWXPayInfo(delegate(WxPayV3.WXPayModel wm)
            {
                //签名后获得的WXPayModel结果对象，此时可以写入订单
                //如果paySign为Error，请检查package内容进行调试和查找错误

            }, openid, tfee, body, pid, sp_billno);
            model.payNo = Guid.NewGuid().ToString("N");
            return JsonConvert.SerializeObject(model);
        }

        public ContentResult Success()
        {
            string result = WxPayV3.ProcessNotify("key", delegate(wxPay.Net.WxPayV3.NotyfyResult res)
            {
                //TODO:回调成功时处理

            }, delegate(wxPay.Net.WxPayV3.NotyfyResult res)
            {
                //TODO:回调失败时处理

            });
            //此处一定要返回，不然微信服务器收不到确认信息
            return Content(result);
        }
    }
}