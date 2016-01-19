# wxpay
微信支付-V3 JQuery插件 支持H5页面支付JSSDK
## 开发环境支持
1.VS+C#+Jquery+Senparc.Weixin.MP
2.有Senparc.Weixin.MP了为什么还要开发这个组件？
  实际上Senparc.Weixin封装了JSSDK的大部分功能，包括微信支付，但是作为该组件的最终应用者（End-User），常常会被他的DEMO迷惑，并且实际开发中会遇到各种“坑”，这个组件就是为了帮助开发人员跳坑，并通过最简单的办法调用以便实现微信支付功能，本人就是从坑里跳出来的人，过来人，说出来都是泪，你懂的！
## 这个组件做什么用的？解决了什么问题？
1.获取微信支付签名信息<br />
    调用前请先配置wxPayV3Info属性，否则会支付失败,如果paySign = "ERROR"，请查看package内容信息<br />
    目前提供最基本的签名字段，有时间的话可以增加基础以外字段的动态增加，一般情况下，基础的字段也能满足支付需求了<br />
    微信支付步骤：<br />
    a.$.post调用GetWXPayInfo()获取WXPayModel数据，其中包含签名数据<br />
    b.wxpay.js调用 WeixinJSBridge.invoke('getBrandWCPayRequest')发起微信支付<br />
    c.处理回调，成功后返回success<br />
##使用wxPay插件H5简单调用微信支付的Demo
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
                pid: 0, param: "", sp_billno: "@BalanceHelper.GenOrderId()", desc: "这是一个测试支付"
            };
            $("a.pay").wxPay("/usercenter/payment", "oGdiZuO-ZyMILKGWG_5ZXC6rSSoE", options, function () {
                return 1;
            }, function () {
                alert("支付成功success");
            }
            );
        });
    </script>
</head>
<body>
    <div>
        <a class="pay" href="javascript:;">微信支付测试</a>
    </div>
</body>
</html>
```
