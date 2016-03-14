﻿; $(function ($, window, undefined) {
    'use strict';
    $.fn.wxPay = function (signurl, openid, options, paybefore, success, fail, cancel) {
        var wx_pay = false;
        var defaults = {
            pid: "0", desc: ""
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
            if ($.fn.wxPay.WxVersion() < "5.0") {
                alert("请升级微信客户端");
                return;
            }
            if (settings.desc == "") settings.desc = "备注不能为空-wxpay.net";
            $(this).click(function () {
                abortpay = false;
                if (!wx_pay && !$.fn.wxPay.Debug) { return; } //alert("正在初始化支付控件或为非微信内置浏览器环境，请稍候再试！");
                var fee = 0;
                if (typeof (paybefore) == "function") {
                    var rfee = paybefore();
                    if (rfee == null || typeof (rfee) == "undefined") abortpay = true; //中止支付                        
                    if (!isNaN(parseFloat(rfee)))
                        fee = rfee * 100; //转换成分
                }
                if (!abortpay && fee > 0) {
                    $.post(signurl, { openid: openid, tfee: fee, body: settings.desc, pid: settings.pid, param: $.fn.wxPay.OrderParam, sp_billno: $.fn.wxPay.OrderCode }, function (data) {
                        if (data.paySign == "ERROR") {
                            if (typeof (fail) == "function") fail(data.package);
                        }
                        else {
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
                                        if (typeof (fail) == "function") fail(res.err_desc);
                                    } else {
                                        if (typeof (cancel) == "function") cancel(data.payNo);
                                    }
                            });
                        }
                    }, "json");
                }
            });
        }
    };
    $.fn.SelectAddress = function (appid, addsign, timestamp, noncestr, success, fail, cancel) {
        if ($.fn.wxPay.WxVersion() < "5.0") {
            alert("请升级微信客户端");
        } else {
            $(this).click(function () {
                WeixinJSBridge.invoke("editAddress",
                        {
                            'appId': appid,
                            'scope': 'jsapi_address',
                            'signType': 'sha1',
                            'addrSign': addsign,
                            'timeStamp': timestamp,
                            'nonceStr': noncestr
                        },
                            function (res) {
                                if (res.err_msg == "edit_address:ok") {
                                    if (typeof (success) == "function") {
                                        $.fn.wxPay.SelectedAddr = res.proviceFirstStageName + " " + res.addressCitySecondStageName + " " + res.addressCountiesThirdStageName + " " + res.addressDetailInfo;
                                        success(res);
                                    }
                                } else
                                    if (res.err_msg == "edit_address:fail") {
                                        if (typeof (fail) == "function") fail(res.err_desc);
                                    } else {
                                        if (typeof (cancel) == "function") cancel('用户取消');
                                    }
                            }
                    );
            });
        }
    };
    $.fn.wxPay.SelectedAddr = "";
    $.fn.wxPay.OrderParam = "";
    $.fn.wxPay.OrderCode = "";
    $.fn.wxPay.Debug = false;
    $.fn.wxPay.WxVersion = function () {
        var weInfo = navigator.userAgent.match(/MicroMessenger\/([\d\.]+)/i);
        if (!weInfo)
            return 0;
        else
            return weInfo[1];
    };
})(jQuery, window);