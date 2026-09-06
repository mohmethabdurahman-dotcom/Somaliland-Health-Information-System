if (!window['ajax']) {
    window['ajax'] = function (inUrl, inData, inSuccessFunc, inErrorFunc, inTimeout) {

        var timeoutPeriod = inTimeout || 2000000;
        timeoutPeriod = timeoutPeriod <= 0 ? 2000000 : timeoutPeriod;
        var relativeUrl = APPLICATION_ROOT + inUrl;
        var ajaxexec =
            $.ajax({
                cache: false,
                url: relativeUrl,
                data: inData,
                type: "POST",
                async: true,
                dataType: 'text',
                timeout: timeoutPeriod,
                headers: { "X-Requested-With": "XMLHttpRequest" },
                success: function (msg) {
                    if (typeof msg === 'string' && /TW-SOL HIS Login|not signed in/i.test(msg)) {
                        window.location.href = (typeof APPLICATION_ROOT === 'string' ? APPLICATION_ROOT : '/') + 'Login/NotLogin';
                        return;
                    }
                    if (inSuccessFunc != null)
                        inSuccessFunc(msg);
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    if (xhr.status == 401) {
                        window.location.href = (typeof APPLICATION_ROOT === 'string' ? APPLICATION_ROOT : '/') + 'Login/NotLogin';
                        return;
                    }
                    if (inErrorFunc != null)
                        inErrorFunc();
                    var statusText = (xhr && xhr.status) ? xhr.status : 'network';
                    layer.alert("Server error. Please try again. (Error: " + statusText + (thrownError ? " - " + thrownError : "") + ")", {
                        skin: 'layui-layer-lan',
                        closebtn: 1,
                        anim: 5,
                        icon: 2,
                        btn: ['OK'],
                        title: 'Error Message'
                    });
                }
            });
        return ajaxexec;
    };
}

if (!window['ajaxGet']) {
    window['ajaxGet'] = function (inUrl, inData, inSuccessFunc, inErrorFunc, inTimeout) {

        var timeoutPeriod = inTimeout || 20000;
        timeoutPeriod = timeoutPeriod <= 0 ? 20000 : timeoutPeriod;
        var relativeUrl = APPLICATION_ROOT + inUrl;
        var ajaxexec =
            $.ajax({
                cache: false,
                url: relativeUrl,
                data: inData,
                type: "GET",
                async: true,
                dataType: 'text',
                timeout: timeoutPeriod,
                headers: { "X-Requested-With": "XMLHttpRequest" },
                success: function (msg) {
                    if (typeof inSuccessFunc === 'function')
                        inSuccessFunc(msg);
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    if (xhr.status == 401) {
                        window.location.href = (typeof APPLICATION_ROOT === 'string' ? APPLICATION_ROOT : '/') + 'Login/NotLogin';
                        return;
                    }
                    if (inErrorFunc != null)
                        inErrorFunc();
                    var statusText = (xhr && xhr.status) ? xhr.status : 'network';
                    layer.alert("Server error. Please try again. (Error: " + statusText + (thrownError ? " - " + thrownError : "") + ")", {
                        skin: 'layui-layer-lan',
                        closebtn: 1,
                        anim: 5,
                        icon: 2,
                        btn: ['OK'],
                        title: 'Error Message'
                    });
                }
            });
        return ajaxexec;
    };
}
