# wxpay
微信支付-V3 JQuery插件 支持H5页面支付JSSDK
H5调用Demo
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
            $("a.pay").wxPay("/usercenter/payment", "oGdiZ*****SSoE", 1, {
                desc: "这是一个测试支付", pid: 0, param: "", success: function () {
                    alert("支付成功success");
                }
            });
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
