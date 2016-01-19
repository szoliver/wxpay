; $(function ($, window, undefined) {
    'use strict';
    $.fn.wxPay = function (signurl, openid, options, paybefore, success, fail, cancel) {
        var wx_pay = false;
        var defaults = {
            pid: "0", param: "", sp_billno: "", desc: ""
        };
        var settings = $.extend({}, defaults, options);
        var abortpay = false;
        var onBridgeReady = function () { wx_pay = true; }
        if (typeof WeixinJSBridge == "undefined") {
            if (document.addEventListener) {
                document.addEventListener('WeixinJSBridgeReady', onBridgeReady, false);
            } else if (document.attachEvent) {
                document.attachEvent('WeixinJSBridgeReady', onBridgeReady);
                document.attachEvent('onWeixinJSBridgeReady', onBridgeReady);
            }
        } else { onBridgeReady(); }


        if (typeof (openid) == "undefined" || openid == '')
            alert('OpenID错误');
        else {
            this.click(function () {
                if (!wx_pay) { return; } //alert("正在初始化支付控件或为非微信内置浏览器环境，请稍候再试！");
                var fee = 0;
                if (typeof (paybefore) == "function") {
                    var rfee = paybefore();
                    if (rfee == null || typeof (rfee) == "undefined") abortpay = true; //中止支付                        
                    if (!isNaN(parseFloat(rfee)))
                        fee = rfee;
                }
                if (!abortpay && fee > 0) {
                    $.post(signurl, { openid: openid, tfee: fee, body: settings.desc, pid: settings.pid, param: settings.param, sp_billno: settings.sp_billno }, function (datastr) {
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
                                if (typeof (success) == "function") success();
                            } else
                                if (res.err_msg == "get_brand_wcpay_request:fail") {
                                    if (typeof (fail) == "function") fail();
                                } else {
                                    if (typeof (cancel) == "function") cancel(res.err_msg);
                                }
                        });
                    });
                }
            });
        }
    };
})(jQuery, window);