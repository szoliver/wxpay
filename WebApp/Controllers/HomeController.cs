using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wxPay.Net;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
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
            WxPayV3.WXPayModel model = WxPayV3.GetWXPayInfo(delegate(WxPayV3.WXPayModel wm, string _openid, string _tfee, string _body, string _pid, string _param, string _sp_billno)
            {
                //签名后获得的WXPayModel结果对象，此时可以写入订单

            }, openid, tfee, body, pid, param, sp_billno);
            string modstr = JsonConvert.SerializeObject(model);
            return modstr;
        }
 
    }
}