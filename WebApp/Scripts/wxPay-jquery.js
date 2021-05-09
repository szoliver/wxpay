; (function ($, window) {
    'use strict';
    $.fn.wxPay = function (signurl, openid, options, paybefore, success, fail, cancel) {
        var wx_pay = false;
        var defaults = {
            pid: "0", desc: "支付费用"
        };
        var settings = $.extend({}, defaults, options);
        var onBridgeReady = function () { wx_pay = true; }
        if (typeof WeixinJSBridge == "undefined") {
            if (document.addEventListener) {
                document.addEventListener('WeixinJSBridgeReady', onBridgeReady, false);
            } else if (document.attachEvent) {
                document.attachEvent('WeixinJSBridgeReady', onBridgeReady);
                document.attachEvent('onWeixinJSBridgeReady', onBridgeReady);
            }
        } else { onBridgeReady(); }
        var yuan2fen=function (r) {
            var m = 0, s1 = r.toString(), s2 = "100";
            try { m += s1.split(".")[1].length } catch (e) { }
            try { m += s2.split(".")[1].length } catch (e) { }
            return Number(s1.replace(".", "")) * Number(s2.replace(".", "")) / Math.pow(10, m);
        };
        var compare=function(curV,reqV){
            if(curV && reqV){
               var arr1 = curV.split('.'),
                   arr2 = reqV.split('.');
               var minLength=Math.min(arr1.length,arr2.length),
                   position=0,
                   diff=0;
               while(position<minLength && ((diff=parseInt(arr1[position])-parseInt(arr2[position]))==0)){
                   position++;
               }
               diff=(diff!=0)?diff:(arr1.length-arr2.length);
               return diff>0;
            }else{
               return false;
            }
        };
        if ($.isEmptyObject(openid)&&!$.fn.wxPay.Debug){
            $(this).click(function(){
                alert('用户信息错误，无法调用微信支付');
            });
            $(this).click();
        }else {
            var ver=$.fn.wxPay.WxVersion();
            if (!compare(ver,"8.0.3")&&!$.fn.wxPay.Debug) {
                $(this).click(function(){
                    alert("请安装并升级微信APP，版本>=8.0.3，当前："+(ver=="0"?"非微信浏览器":ver));
                });
                $(this).click();
            }else{
                if (settings.desc == "") settings.desc = "备注不能为空";else
                {
                    var canRun = true;                    
                    $(this).click(function () {
                        if(canRun){
                            canRun = false;
                            if (!wx_pay && !$.fn.wxPay.Debug) {canRun = true; return; }
                            var fee = 0;
                            if (typeof paybefore === "function") {
                                var rfee = paybefore();                        
                                if(isNaN(rfee)){canRun = true;return;} //如果返回的金额是NaN，取消支付流程                                                        
                                if (parseFloat(rfee)>0)
                                    fee = yuan2fen(rfee).toFixed(0); //转换成分
                                else
                                    fee=rfee;
                            }
                            if(typeof paybefore === 'number' && !isNaN(paybefore))
                                fee=yuan2fen(paybefore).toFixed(0);
                            if (!(fee==0)) { //如果金额小于0，让后台决定金额，应用场景可以是例如固定或有规则金额的情况下
                                $.post(signurl, { openid: openid, tfee: fee, body: settings.desc, pid: settings.pid, param: $.fn.wxPay.OrderParam, sp_billno: $.fn.wxPay.OrderCode }, function (data) {
                                    if (data.paySign == "ERROR") {
                                        if (typeof fail === "function") fail(data.package);
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
                                                if (typeof success === "function") success({fee:data.fee,ordercode:data.ordercode});
                                            } else
                                                if (res.err_msg == "get_brand_wcpay_request:fail") {
                                                    if (typeof fail === "function") fail(res.err_desc);
                                                } else {
                                                    if (typeof cancel === "function") cancel(data.payNo);
                                                }
                                        });
                                    }
                                }, "json");
                            }                            
                            setTimeout(function(){canRun = true;}, 800);
                        }
                    });                    
                }
            }
        }
    };
    $.fn.SelectAddress = function (appid, addsign, timestamp, noncestr, success, fail, cancel) {
        if ($.fn.wxPay.WxVersion() < 5) {
            alert("请使用或升级微信客户端");
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
                                    if ($.isFunction(success)) {
                                        $.fn.wxPay.SelectedAddr = res.proviceFirstStageName + " " + res.addressCitySecondStageName + " " + res.addressCountiesThirdStageName + " " + res.addressDetailInfo;
                                        success(res);
                                    }
                                } else
                                    if (res.err_msg == "edit_address:fail") {
                                        if ($.isFunction(fail)) fail(res.err_desc);
                                    } else {
                                        if ($.isFunction(cancel)) cancel('用户取消');
                                    }
                            }
                    );
            });
        }
    };
    $.fn.wxPay.SelectedAddr = "";
    $.fn.wxPay.OrderParam = "";
    $.fn.wxPay.OrderCode = ""; //为空，让系统生成并返回
    $.fn.wxPay.Debug = false;
    $.fn.wxPay.WxVersion = function () {
        var weInfo = navigator.userAgent.match(/MicroMessenger\/([\d\.]+)/i);
        if (!weInfo)
            return "0";
        else
            return weInfo[1];
    };
})(jQuery, window);
