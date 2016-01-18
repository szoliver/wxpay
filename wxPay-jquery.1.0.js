; $(function ($, window, document, undefined) {
    $.fn.wxPay = function (signurl, openid, payfee, options) {
        var errdesc = 'null';
        var defaults = {
            'desc': 'weixin_pay_order:' + openid,
            'success': function () { alert('success:' + errdesc); },
            'fail': function () { alert('fail:' + errdesc); },
            'cancel': function () { alert('cancel:' + errdesc); }
        };
        var settings = $.extend({}, defaults, options);
        var IsSupportWxPay = function () { return (typeof WeixinJSBridge == "undefined"); }
        var fee = parseFloat(payfee);
        if (!IsSupportWxPay)
            alert("此浏览器不支持微信支付，请进入微信支付！");
        else {
            if (openid == undefined || openid == '')
                alert('OpenID错误');
            else if (fee == NaN)
                alert('金额错误');
            else {
                this.click(function () {
                    $.post(signurl, { openid: openid, tfee: fee, body: settings.desc }, function (datastr) {
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
                                settings.success();
                            } else
                                if (res.err_msg == "get_brand_wcpay_request:fail") {
                                    settings.fail();
                                } else {
                                    settings.cancel(res.err_msg);
                                }
                        });
                    });
                });
            }
        }
    };
})(jQuery, window, document);
