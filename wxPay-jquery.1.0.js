; $(function ($, window, document, undefined) {
    $.fn.wxPay = function (signurl, openid, payfee, pid, param, desc, paybefore, success, fail, cancel) {
        var IsSupportWxPay = function () { return !(typeof WeixinJSBridge == "undefined"); }
        var fee = parseFloat(payfee);
        if (openid == undefined || openid == '')
            alert('OpenID错误');
        else if (isNaN(fee))
            alert('金额错误');
        else {
            this.click(function () {
                if (!IsSupportWxPay())
                    alert("此浏览器不支持微信支付，请进入微信支付！");
                else {
                    if (paybefore) {
                        var rfee = parseFloat(paybefore());
                        if (!isNaN(rfee))
                            fee = rfee;
                    }
                    $.post(signurl, { openid: openid, tfee: fee, body: desc, pid: pid, param: param }, function (datastr) {
                        var data = eval("(" + datastr + ")");
                        WeixinJSBridge.invoke(
                        'getBrandWCPayRequest', {
                            "appId": data.appId, //公众号名称，由商户传入
                            "timeStamp": data.timeStamp, //时间戳
                            "nonceStr": data.nonceStr, //随机串
                            "package": data.package,//扩展包
                            "signType": "MD5", //微信签名方式
                            "paySign": data.paySign //微信签名
                        }, function (res) {
                            if (res.err_msg == "get_brand_wcpay_request:ok") {
                                if (success) success();
                            } else
                                if (res.err_msg == "get_brand_wcpay_request:fail") {
                                    if (fail) fail();
                                } else {
                                    if (cancel) cancel(res.err_msg);
                                }
                        });
                    });
                }
            });
        }
    };
})(jQuery, window, document);
