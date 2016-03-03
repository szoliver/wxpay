# wxpay V1.2.2
微信支付-V3 JQuery插件 支持H5页面支付JSSDK<br />
本组件是基于Senparc.Weixin.MP的应用，人家都已经封装得很好了，就没必要再封装一次，只是为了简化Senparc微信支付的开发应用，开发人员把注意力只放在业务处理上，而不必再纠结如何使用Senparc.Weixin.MP进行微信开发的问题。
## 开发环境支持
1.VS+C#+Jquery+Senparc.Weixin.MP(不会搞php的)<br />
2.有Senparc.Weixin.MP了为什么还要开发这个组件？<br />
  实际上Senparc.Weixin封装了JSSDK的大部分功能，包括微信支付，但是作为该组件的最终应用者（End-User），常常会被他的DEMO迷惑，并且实际开发中会遇到各种“坑”，这个组件就是为了帮助开发人员跳坑，并通过最简单的办法调用以便实现微信支付功能，本人就是从坑里跳出来的人，过来人，说出来都是泪，你懂的！
## NuGet包已经发布
```nuget
Install-Package wxPay.Net
```
或搜索“微信支付”，全称"微信支付开发组件-wxPay"，
## 这个组件做什么用的？解决了什么问题？
1.获取微信支付签名信息<br />
    调用前请先配置wxPayV3Info属性，否则会支付失败,如果paySign = "ERROR"，请查看package内容信息<br />
    目前提供最基本的签名字段，有时间的话可以增加基础以外字段的动态增加，一般情况下，基础的字段也能满足支付需求了<br />
    微信支付步骤：<br />
    a.$.post调用GetWXPayInfo()获取WXPayModel数据，其中包含签名数据<br />
    b.wxpay.js调用 WeixinJSBridge.invoke('getBrandWCPayRequest')发起微信支付<br />
    c.处理回调，成功后返回success<br />
## 源码位置
  wxPay js源码在[wxPay-jquery.js](https://github.com/szoliver/wxpay/blob/master/WebApp/Scripts/wxPay-jquery.js)<br />
  wxPay.Net在此同名目录下<br />
## wxPay功能特色
   1.获取微信收货地址<br />
   2.本质上是经验积累，跳过那些坑，可以快速完成微信支付开发（H5公众号）<br />
   3.js快速调用<br />
   $("a.pay").wxpay(...)，前端这一行代码即可完成微信支付功能，引入wxpay.js（Jquery插件）即可。<br />
   上行代码中a.pay为点击支付的按钮Jq选择器 <br />
   4.后台处理简便<br />
   支持自动签名处理（GetWXPayInfo）和回调处理（ProcessNotify），通过方法委托可以编写自己的业务代码，Demo中有详细的代码供参考。<br />
   
## 使用wxPay插件H5简单调用微信支付的Demo
```html
<!DOCTYPE html>

<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Index</title>
    @Scripts.Render("~/bundles/jquery")
    <script src="~/Content/js/wxPay-jquery.js"></script>
    <script>
        $(function () {
            var options = {
                pid: 0, sp_billno: "@BalanceHelper.GenOrderId()", desc: "这是一个测试支付"
            };
            $("a.pay").wxPay("/usercenter/payment", "oGdiZuO-ZyMILKGWG_5ZXC6rSSoE", options, function () {
                $.fn.wxPay.OrderParam=""; //附加数据可以在/usercenter/payment中处理
                return 1;
            }, function () {
                alert("支付成功success");
            }
            );
            //取accesstoken，非组件中的var accessToken = AccessTokenContainer.TryGetAccessToken(appId, appSecret);，切记切记！！
            //本例中进行了授权跳转，取到code才能取到OAuth2的AccessToken
            //本视图对应的方法中进行了跳转，详见DEMO
            //或是你有其他方法取到OAuth2的accesstoken也行，取到后用WxPayV3.GetUserAddrSign签名，得到addSign才是关键
            @{
                string AppID = "";
                string AppSecret = "";
                string url = Request.Url.AbsoluteUri;
                string timestamp = JSSDKHelper.GetTimestamp();
                string nonestr = JSSDKHelper.GetNoncestr();
                var result = OAuthApi.GetAccessToken(AppID,AppSecret, Request.QueryString["code"]);
                string accesstok =result.access_token;
                string addSign = WxPayV3.GetUserAddrSign(AppID,accesstok, nonestr, timestamp, url);
            }        
        $("a#getaddr").SelectAddress('@AppID', '@addSign', '@timestamp', '@nonestr', function (res) {
            //其他地址数据：res.userName 收货人 res.telNumber 收货电话 res.addressPostalCode 邮编 res.nationalCode 国家码 
            alert($.fn.wxPay.SelectedAddr);
        }, function (desc) { alert(desc); }, function (desc) { alert(desc); });
        
        });
    </script>
</head>
<body>
    <div>
        <a class="pay" href="javascript:;">微信支付测试</a>
        <a id="getaddr" href="javascript:;">选择微信收货地址</a>
    </div>
</body>
</html>
```
# wxPay的部署流程（演示代码）
## [1].引入wxPay.js 
```html
<script src="~/Content/js/wxPay-jquery.js"></script>
```
## [2].NuGet安装wxPay.Net组件并引用
NuGut控制台：<br /> PM> Install-Package wxPay.Net
 或
NuGet程度包管理器：<br />
![image](https://github.com/szoliver/wxpay/blob/master/WebApp/Content/wxpay_nugut.png)
 ```c#
 using wxPay.Net;
 ```
## [3].生成后台响应代码及URL
```C#
[HttpPost]
        public string Payment(string openid, string tfee, string body, string pid, string param, string sp_billno)
        {
            WxPayV3.wxPayV3Info = new Senparc.Weixin.MP.TenPayLibV3.TenPayV3Info("wx7******983da2d8e4", wxDefine.appsecret, wxDefine.MchId, wxDefine.WxKey, wxDefine.PayNotifyUrl);
            WxPayV3.WXPayModel model = WxPayV3.GetWXPayInfo(delegate(WxPayV3.WXPayModel wm)
            {
                //签名后获得的WXPayModel结果对象，此时可以写入订单
                //如果paySign为Error，请检查package内容进行调试和查找错误
                //写入一个临时订单记录，记录订购信息
                //pid查询产品信息 ，openid 查用户信息
                //param中的数据格式是券的信息类似于"1:122.00"
                int ppid = 0;
                int.TryParse(pid, out ppid);
                y5_user user = DB.Context.From<y5_user>().Where(k => k.yuWXid == openid).First();
                y5_suproduct product = DB.Context.From<y5_suproduct>().Where(k => k.id == ppid).First();
                int oid = BalanceHelper.CreateOrder(user, product, param, sp_billno, tfee, -1);

            }, openid, tfee, body, pid, sp_billno);
            return JsonConvert.SerializeObject(model);

        }
```
## [4].JQuery发起签名请求并处理
```javascript
$(function () {
            var options = {
                pid: 0, sp_billno: "@BalanceHelper.GenOrderId()", desc: "这是一个测试支付"
            };
            $("a.pay").wxPay("/usercenter/payment", "oGdiZuO-ZyMILKGWG_5ZXC6rSSoE", options, function () {
                $.fn.wxPay.OrderParam="";
                return 1;
            }, function () {
                alert("支付成功success");
            }
            );
        });
```
## [5].处理支付通知信息
```c#
public ContentResult NotifyUrl()
        {
            string key = wxDefine.WxKey;
            string result = WxPayV3.ProcessNotify(key, delegate(wxPay.Net.WxPayV3.NotyfyResult res)
            {
                LogHelper.Debug("NotifyUrl:" + res.Content, "PAYDATA_wx");
                //TODO:回调成功时处理
                decimal tfee = 0;
                decimal.TryParse(res.total_fee, out tfee);
                y5_preorder preorder = new y5_preorder()
                {
                    addtime = DateTime.Now,
                    appid = res.appid,
                    fee_type = res.fee_type,
                    is_subscribe = res.is_subscribe,
                    mch_id = res.mch_id,
                    openid = res.openid,
                    out_trade_no = res.out_trade_no,
                    result_code = res.result_code,
                    time_end = res.time_end,
                    total_fee = tfee
                };
                DB.Context.Insert<y5_preorder>(preorder);
                string out_trade_no = res.out_trade_no;
                //修改库存订单信息
                y5_orderlist order = DB.Context.From<y5_orderlist>().Where(k => k.yoOrderCode == out_trade_no).First();
                order.yoPayed = tfee;
                order.yoPayDate = DateTime.Now;
                order.yoStatus = 0;
                DB.Context.Update<y5_orderlist>(order);
            }, delegate(wxPay.Net.WxPayV3.NotyfyResult res)
            {
                //TODO:回调失败时处理

            });
            //此处一定要返回，不然微信服务器收不到确认信息
            return Content(result);
        }
```
