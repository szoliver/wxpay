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
