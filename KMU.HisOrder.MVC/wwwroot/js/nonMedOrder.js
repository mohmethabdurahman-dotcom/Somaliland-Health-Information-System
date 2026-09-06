/// <reference path="..\lib\jquery\dist\jquery.js" />

var oldNonMed = [];

var NONMED_URGENCY_TO_FLAG = {
    'Routine': '1',
    'Urgent': '2',
    'Emergency': '3'
};

var NONMED_URGENCY_FLAG_TO_LABEL = {
    '1': 'Routine',
    '2': 'Urgent',
    '3': 'Emergency'
};

function fn_mapNonMedUrgencyToFlag(urgencyLabel) {
    return NONMED_URGENCY_TO_FLAG[urgencyLabel] || '1';
}

function fn_mapNonMedFlagToUrgency(flag) {
    return NONMED_URGENCY_FLAG_TO_LABEL[String(flag || '1')] || 'Routine';
}

function fn_applyNonMedBloodRowStyle($tr) {
    if (!$tr || $tr.length === 0) return;

    var hplanType = ($tr.find('td[data-plan-type]').attr('data-plan-type')
        || $tr.find('td[data-hplan-type]').attr('data-hplan-type') || '').trim();

    $tr.removeClass('blood-order-row nonmed-urgency-routine nonmed-urgency-urgent nonmed-urgency-emergency');

    if (hplanType !== 'Blood') return;

    var urgency = ($tr.find('select.nonmed-urgency-select').val() || 'Routine').trim();
    $tr.addClass('blood-order-row');

    if (urgency === 'Emergency') {
        $tr.addClass('nonmed-urgency-emergency');
    } else if (urgency === 'Urgent') {
        $tr.addClass('nonmed-urgency-urgent');
    } else {
        $tr.addClass('nonmed-urgency-routine');
    }

    var $urgencySelect = $tr.find('select.nonmed-urgency-select');
    $urgencySelect.removeClass('nonmed-urgency-routine nonmed-urgency-urgent nonmed-urgency-emergency');
    if (urgency === 'Emergency') {
        $urgencySelect.addClass('nonmed-urgency-emergency');
    } else if (urgency === 'Urgent') {
        $urgencySelect.addClass('nonmed-urgency-urgent');
    } else {
        $urgencySelect.addClass('nonmed-urgency-routine');
    }
}

function fn_configureNonMedBloodFields($tr, isBlood) {
    var $bloodSelect = $tr.find('select.nonmed-blood-type-select');
    var $urgencySelect = $tr.find('select.nonmed-urgency-select');

    if (isBlood) {
        $bloodSelect.prop('disabled', false);
        $urgencySelect.prop('disabled', false);
        if (!$bloodSelect.val()) {
            $bloodSelect.val('');
        }
        if (!$urgencySelect.val()) {
            $urgencySelect.val('Routine');
            $urgencySelect.attr('data-urgency', 'Routine');
            $urgencySelect.attr('data-urgency-flag', '1');
        }
        fn_applyNonMedBloodRowStyle($tr);
    } else {
        $bloodSelect.prop('disabled', true).val('');
        $urgencySelect.prop('disabled', true).val('Routine');
        $tr.removeClass('blood-order-row nonmed-urgency-routine nonmed-urgency-urgent nonmed-urgency-emergency');
    }
}

function fn_readNonMedOrderFromRow($tr) {
    return {
        Orderplanid: $tr.attr('data-orderplan-id'),
        PlanCode: $tr.find('td[data-plan-code]').text(),
        PlanDes: ($tr.find('td[data-plan-des]').attr('data-plan-des') || $tr.find('td[data-plan-des]').clone().children().remove().end().text()).trim(),
        QtyDose: $tr.find('input[data-plan-qty]').attr('data-plan-qty'),
        DosePath: ($tr.find('select.nonmed-blood-type-select').val() || '').trim(),
        UrgFlag: fn_mapNonMedUrgencyToFlag($tr.find('select.nonmed-urgency-select').val()),
        LocationCode: $tr.find('td > select[data-location-code]').attr('data-location-code'),
        Remark: $tr.find('input[data-remark]').attr('data-remark'),
        HplanType: ($tr.find('td[data-plan-type]').attr('data-plan-type')
            || $tr.find('td[data-hplan-type]').attr('data-hplan-type') || '').trim()
    };
}

function getRandomData(x) {

    var index = Math.floor(Math.random() * x);

    var list = [
        { orderplaid: '', plancode: '303456', plandes: 'CBC-1', qty: 1 },
        { orderplaid: '', plancode: '111111', plandes: 'Chest-Pa', qty: 1 },
        { orderplaid: '', plancode: '222222', plandes: 'Ca', qty: 1 },
        { orderplaid: '', plancode: '333333', plandes: 'Na', qty: 1 },
        { orderplaid: '', plancode: '777777', plandes: 'MRI', qty: 1 },
        { orderplaid: '', plancode: '33072B', plandes: 'CT', qty: 1 }
    ];

    var newList = [];
    newList.push(list.at(index));

    return newList;
};



//初始化
function init_NonMedOrder() {

    //table
    var table = $("#nonmedorder_table");

    //changeFlag重製
    changeObj.NonMed = false;


    //fn_addNonMedorder(list)
    //顯示筆數
    fn_showNonMedOrderCount();
    //設定icheck屬性
    fn_seticheck(table);
    //置底
    /*  table.scrollTop(table.height());*/


    $(".add_nonmedorders").click(function () {
        fn_addNonMedorder(getRandomData(5))
    });


    $(".delete_nonmedorders").click(function () {
        fn_deleteNonMedOrders(table)
    });

    $(".completed_nonmedorders").click(function () {
    });


    $("#NonMedPrint").click(function () {
        //取得tr
        var checklist = table.find("input:checked:not(.all):not(.nonmedorder_tr_templete)");
        var idlist = [];
        if (checklist.length > 0) {
            checklist.each(function (idx, val) {

                var orderplaid = $(val).closest('tr').attr("data-orderplan-id");
                var modifytype = $(val).closest('tr').attr('data-modify-type');


                if (orderplaid == "-1" || modifytype == "U") {

                    layer.alert('Only archived orders may be reprinted', {
                        skin: 'layui-layer-lan',
                        closebtn: 1,
                        anim: 5,
                        icon: 2,
                        btn: ['OK'],
                        title: 'Message'
                    }, function (index) {
                        layer.close(index);

                    });
                    idlist.length = 0;
                    return false;

                } else {
                    idlist.push(orderplaid);
                    /* $(val).closest('tr:not(.nonmedorder_tr_templete)').attr("hidden", true).attr("data-modify-type", "D");*/
                }
            });


            if (idlist != undefined && idlist.length > 0) {
                fn_RePrint(idlist);
            }

            //changeObj.NonMed = true;
            //fn_CheckBeforeUnload();
        } else
        {
            layer.alert('1. Select the item you want to reprint. </br> 2. Only archived orders may be reprinted.', {
                skin: 'layui-layer-lan',
                closebtn: 1,
                anim: 5,
                icon: 2,
                btn: ['OK'],
                title: 'Message'
            }, function (index) {
                layer.close(index);

            });
            
            
        }



        //取消checkbox
        table.find('input.all').iCheck('uncheck');
        //計數
        fn_showNonMedOrderCount();

    });


    //用不到了
    //$(".save_nonmedorders").click(function (event, status) {
    //    fn_SaveNonMedOrderByElements(status);
    //});

    table.find('input[data-plan-qty]').change(function () {

        $(this).attr('data-plan-qty', $(this).val());
        fn_checkNonMedOrderChange($(this));
    });

    table.find('input[data-remark]').change(function () {

        $(this).attr('data-remark', $(this).val());
        fn_checkNonMedOrderChange($(this));
    });

    table.find('td > select[data-location-code]').change(function () {
        $(this).attr('data-location-code', $(this).val());
        fn_checkNonMedOrderChange($(this));
    });

    table.find('select.nonmed-blood-type-select').change(function () {
        $(this).attr('data-blood-type', $(this).val());
        fn_checkNonMedOrderChange($(this));
    });

    table.find('select.nonmed-urgency-select').change(function () {
        var urgency = $(this).val();
        $(this).attr('data-urgency', urgency);
        $(this).attr('data-urgency-flag', fn_mapNonMedUrgencyToFlag(urgency));
        fn_applyNonMedBloodRowStyle($(this).closest('tr'));
        fn_checkNonMedOrderChange($(this));
    });

    table.find('input[data-plan-qty]').focusout(function (e) {
        checkInput($(this));
    });

    table.find('input[data-plan-qty]').keyup(function (e) {
        if (e.keyCode == 13) {

            var result = checkInput($(this));
            if (!result) {
                return;
            }

            //fn_checkMedOrderChange($(this));

            var tt = $(this).closest('tr').next();
            if ($(this).closest('tr').next().length > 0) {
                $(this).closest('tr').next().find('input[data-plan-qty]').focus().select();
            } else {
                $(this).closest('tr').siblings('tr:visible:first').find('input[data-plan-qty]').focus().select();
            }
        }
    });


    //正則
    function checkInput(e) {
        var input = $(e).val();
        var pattern = /^(\d{1,5}(\.\d{1,2})?|0(\.\d{1,2})?)?$/;

        if (pattern.test(input)) {
            if ($(e).hasClass('border-danger')) {
                $(e).removeClass('border-danger');
            }

            return true;

        } else {

            if ($(e).hasClass('border-danger') == false) {
                $(e).addClass('border-danger');
            }

            $(e).val(function (index, value) {
                return value.replace(value, '');
            });

            $(e).trigger('change');

            layer.tips('<div style="font-size:16px;">Input error: </div> <div style="font-size:14px;"> Please enter the correct value </div>', e, { tips: [2, "#dc3545"], time: 3000 });

            return false;
        }
    }


    fn_reload_old_nonmedorder_data();

    table.find('tbody > tr:not(.nonmedorder_tr_templete)').each(function () {
        fn_applyNonMedBloodRowStyle($(this));
    });

    fn_CheckBeforeUnload();

}

function fn_checkNonMedOrderChange(element) {

    var changeFlag = false;

    var $tr = $(element).closest('tr');

    var oldData = oldNonMed.find(({ Orderplanid }) => Orderplanid == $tr.attr('data-orderplan-id'));

    var newOrder = fn_readNonMedOrderFromRow($tr);

    //欄位是否異動-擴充
    if (oldData != undefined &&
        ((oldData.QtyDose != newOrder.QtyDose) ||
        (oldData.LocationCode != newOrder.LocationCode) ||
        (oldData.Remark != newOrder.Remark) ||
        ((oldData.DosePath || '') != (newOrder.DosePath || '')) ||
        (String(oldData.UrgFlag || '1') != String(newOrder.UrgFlag || '1')))
    ) {
        changeFlag = true;
    }


    if (changeFlag) {
        var $span = $tr.find('td>span[data-status]');

        if ($span.attr('data-status') == '0' || $span.attr('data-status') == '2') {
            if ($span.attr('data-status') == '0') {
                $span.removeClass('badge-gray');
            } else {
                $span.removeClass('badge-success');
            }

            $span.addClass('badge-warning').text('Change');

            $tr.attr("data-modify-type", 'U');

        }
    } else {

        var $span = $tr.find('td>span[data-status]');

        if ($span.hasClass('badge-warning')) {
            $span.removeClass('badge-warning');
            if ($span.attr('data-status') == '0') {
                $span.addClass('badge-gray').text('Examining');
            } else {
                $span.addClass('badge-success').text('Cfm');
            }

            //還原，不異動
            $tr.attr("data-modify-type", '');
        }
    }

    changeObj.NonMed = changeFlag;
    fn_CheckBeforeUnload();

    return changeFlag;

}

//刪除
function fn_deleteNonMedOrders(table) {

    //取得tr
    var checklist = table.find("input:checked:not(.all):not(.nonmedorder_tr_templete)");

    if (checklist.length > 0) {
        checklist.each(function (idx, val) {

            var orderplaid = $(val).closest('tr').attr("data-orderplan-id");

            if (orderplaid == "") {
                $(val).closest('tr:not(.nonmedorder_tr_templete)').remove();
            } else {
                $(val).closest('tr:not(.nonmedorder_tr_templete)').attr("hidden", true).attr("data-modify-type", "D");
            }
        });

        changeObj.NonMed = true;
        fn_CheckBeforeUnload();
    }



    //取消checkbox
    table.find('input.all').iCheck('uncheck');
    //計數
    fn_showNonMedOrderCount();
}

function fn_seticheck(table) {
    // iCheck
    table.find("input").iCheck({
        labelHover: true,
        cursor: true,
        checkboxClass: "icheckbox_flat-pink",
        radioClass: "iradio_square-blue",
        increaseArea: "15%"
    });

    var checkAll = table.find('input.all:not(.nonmedorder_tr_templete)');
    var checkboxes = table.find('input.check');

    checkAll.on('ifChecked ifUnchecked', function (event) {
        if (event.type == 'ifChecked') {
            checkboxes.iCheck('check');
        } else {
            checkboxes.iCheck('uncheck');
        }
    });

    checkboxes.on('ifChanged', function (event) {

        //勾選變色
        var tr = $(this).closest('tr');
        const value = $(this).iCheck('update')[0].checked;
        if (value) {
            tr.css({ 'background-color': '#FFF0F5' });
        } else {
            tr.css({ 'background-color': '' });
        }


        if (checkboxes.filter(':checked').length == checkboxes.length) {
            checkAll.prop('checked', 'checked');
        } else {
            checkAll.removeProp('checked');
        }
        checkAll.iCheck('update');
    });
}

function fn_showNonMedOrderCount() {
    var dataCount = $("#nonmedorder_table").find("tbody > tr:visible").length
    var strCount = (dataCount == 0) ? "0" : dataCount.toString();
    //顯示筆數
    $("#nonmedorder_count").text(strCount);
}

function fn_reload_nonmedorder_partial_view() {

    var nonmedorder_section = $(".nonmedorder-section");
    nonmedorder_section.empty();
    var vURL = "/HisOrder/NonMed/ReloadPartialView";
    var vSuccessFunc = function (result) {
        /*layer.load();*/
        nonmedorder_section.html(result);
        //重新綁定事件
        init_NonMedOrder();
        /*layer.closeAll('loading');*/
    };
    var vErrorFunc = function () {
        nonmedorder_section.html("載入失敗");
        return this;
    };
    ajaxGet(vURL, null, vSuccessFunc, vErrorFunc);

}


function fn_reload_old_nonmedorder_data() {

    var inhospid = $('#patientInhospid').val();
    var vURL = "/HisOrder/NonMed/GetHisOrderPlan";
    var vData = {
        inhospid: inhospid
    };
    var vSuccessFunc = function (msg) {

        var result = JSON.parse(msg);
        if (result.isSuccess == true) {
            var data = JSON.parse(result.returnValue);
            if (data != null && data != undefined) {
                oldNonMed.length = 0;
                $.each(data, function (k, v) {
                    oldNonMed.push(v);
                });
            }
        }
    };
    var vErrorFunc = function () {

        return this;
    };
    ajaxGet(vURL, vData, vSuccessFunc, vErrorFunc);

}


//畫面add 
function fn_addNonMedorder(DataList) {

    if (DataList !== 'undefined' && DataList.length > 0) {

        var table = $("#nonmedorder_table");
        $.each(DataList, function (index, value) {

            console.log(value.orderplaid);
            console.log(value.plancode);
            console.log(value.plandes);

            var maxseq = table.find("tbody > tr:not(.nonmedorder_tr_templete):last > td[data-seq-no]").attr("data-seq-no");
            if (maxseq == undefined) {
                maxseq = 1;
            } else {
                maxseq++;
            }

            var $el = $(".nonmedorder_tr_templete:first")
                .clone(true, true)
                .removeClass("nonmedorder_tr_templete")
                .attr('hidden', false)
                .attr("data-modify-type", "I");

            $el.find("input.nonmedorder_tr_templete").removeClass("nonmedorder_tr_templete");


            //$el.attr("data-orderplan-id", value.orderplaid)
            //    .attr("data-status", "")
            //    .attr("data-plan-code", value.plancode)
            //    .attr("data-plan-des", value.plandes)
            //    .attr("data-plan-qty", value.qty)
            //    .attr("data-seq-no", "78");


            $el.find("td[data-orderplan-id]").attr('data-orderplan-id', value.orderplaid).text(value.orderplaid);
            $el.find("td[data-plan-code]").attr('data-plan-code', value.plancode).text(value.plancode);
            $el.find("td[data-plan-des]").attr('data-plan-des', value.plandes).text(value.plandes);
            $el.find("td>span[data-status]").attr('data-status', "").addClass("badge-pink").text("?");
            $el.find("input[data-plan-qty]").attr('data-plan-qty', value.qty).val(value.qty);

            if (value.LocationCode != null && String(value.LocationCode).trim() !== '') {
                var loc = String(value.LocationCode).trim();
                var $sel = $el.find("select[data-location-code]");
                $sel.val(loc);
                $sel.attr("data-location-code", loc);
            }
            if (value.Remark != null && String(value.Remark).trim() !== '') {
                var rmk = String(value.Remark).trim();
                $el.find("input[data-remark]").attr("data-remark", rmk).val(rmk);
            }

            var hplanTxt = "";
            var hplanClassColor = ""
            switch (value.NonMedItemType)
            {
                case "Lab":
                    hplanTxt = "Laboratory";
                    hplanClassColor = "badge-purple"
                    break;
                case "Exam":
                    hplanTxt = "Radiology";
                    hplanClassColor = "badge-info"
                    break;
                case "Path":
                    hplanTxt = "Pathology";
                    hplanClassColor = "badge-master"
                    break;
                case "Blood":
                    hplanTxt = "Blood";
                    hplanClassColor = "badge-danger"
                    break;
                default:
                    hplanTxt = "Material";
                    hplanClassColor = "badge-warning"
                    break;
            }

            $el.find("td[data-hplan-type]").attr('data-hplan-type', value.NonMedItemType);

            $el.find("td[data-hplan-type] > span").text(hplanTxt).addClass(hplanClassColor);

            var isBlood = value.NonMedItemType === "Blood";
            fn_configureNonMedBloodFields($el, isBlood);

            if (isBlood) {
                $el.find("td[data-plan-des]").append('<div class="nonmed-blood-route-hint text-muted">Route IV</div>');
                $el.find("select.nonmed-blood-type-select").val('').attr('data-blood-type', '');
                $el.find("select.nonmed-urgency-select")
                    .val('Routine')
                    .attr('data-urgency', 'Routine')
                    .attr('data-urgency-flag', '1');
                fn_applyNonMedBloodRowStyle($el);
            }

            var today = new Date();
            var dateStr = ('0' + today.getDate()).slice(-2) + '/'
                + ('0' + (today.getMonth() + 1)).slice(-2) + '/'
                + today.getFullYear();
            $el.find('td.nonmed-order-date').text(dateStr);

            //hidden
            $el.find("td[data-seq-no]").attr('data-seq-no', maxseq);
            $el.find("td[data-plan-type]").attr('data-plan-type', value.NonMedItemType);
            console.log($el);

            table.find("tbody").append($el);

        });

        changeObj.NonMed = true;
        fn_CheckBeforeUnload();
        fn_seticheck(table);
        fn_showNonMedOrderCount();
        table.scrollTop(table.height());

    }

}

//取得前端資訊，將醫令寫入DB
function fn_SaveNonMedOrderByElements(dfd1, status) {

    var trList = $("#nonmedorder_table").find("tbody > tr:not(.nonmedorder_tr_templete):visible");
    const orderAry = [];
    var bloodValidationError = null;

    trList.each(function (idx, val) {
        var modifyType = $(val).attr("data-modify-type");
        if (modifyType === "D") {
            return;
        }

        var rowData = fn_readNonMedOrderFromRow($(val));

        if (rowData.HplanType === "Blood" && !rowData.DosePath) {
            bloodValidationError = 'Blood orders require a blood type (e.g. B-, O+).';
            return false;
        }

        // Blood already saved (has orderplan id) must never insert again as "I"
        var oid = String($(val).attr("data-orderplan-id") || rowData.Orderplanid || "").trim();
        if (rowData.HplanType === "Blood" && oid && oid !== "-1" && modifyType === "I") {
            modifyType = "U";
        }

        var obj = {
            Orderplanid: rowData.Orderplanid,
            PlanCode: rowData.PlanCode,
            PlanDes: rowData.PlanDes,
            QtyDose: rowData.QtyDose,
            DosePath: rowData.DosePath,
            UrgFlag: rowData.UrgFlag,
            Remark: $(val).find("input[data-remark]").val(),
            SeqNo: $(val).find("td[data-seq-no]").attr("data-seq-no"),
            LocationCode: rowData.LocationCode,
            ModifyType: modifyType,
            HplanType: rowData.HplanType
        };
        orderAry.push(obj);
    });

    if (bloodValidationError) {
        layer.alert(bloodValidationError, {
            skin: 'layui-layer-lan',
            closebtn: 1,
            anim: 5,
            icon: 2,
            btn: ['OK'],
            title: 'Blood order'
        });
        dfd1.reject({ isSuccess: false, Message: bloodValidationError });
        return dfd1.promise();
    }


    var vURL = "/HisOrder/NonMed/ModifyNonMedOrder";
    var vData = {
        inOrder: JSON.stringify(orderAry),
        inStatus: status
    };
    var vSuccessFunc = function (msg) {

        var objResult = JSON.parse(msg);
        /*fn_reload_nonmedorder_partial_view();*/
        //return objResult;
        console.log('nonmed ok');
        dfd1.resolve(objResult);
    };
    var vErrorFunc = function (xhr) {

        return xhr;
    };

    ajax(vURL, vData, vSuccessFunc, vErrorFunc);


    return dfd1.promise();
}


//醫令寫入DB
//function fn_SaveNonMedOrder(orderList, status) {

//    var vURL = "/HisOrder/NonMed/ModifyNonMedOrder";
//    var vData = {
//        inOrder: orderList,
//        inStatus: status
//    };
//    var vSuccessFunc = function (msg) {

//        var objResult = JSON.parse(msg);
//        return objResult;
//    };
//    var vErrorFunc = function (xhr) {

//        return objResult;
//    };
//    ajax(vURL, vData, vSuccessFunc, vErrorFunc);
//}

// Used by clinic AI (NCD server path and general prompt) — mirrors fn_SaveNonMedOrderByElements field reads.
window.extractNonMedOrdersFromTable = function extractNonMedOrdersFromTable() {
    const nonMedList = [];
    $("#nonmedorder_table").find("tbody > tr:not(.nonmedorder_tr_templete)").each(function () {
        const $r = $(this);
        if (!$r.is(":visible") || $r.attr("data-modify-type") === "D") {
            return;
        }

        const hplanType = (
            $r.find("td[data-plan-type]").attr("data-plan-type")
            || $r.find("td[data-hplan-type]").attr("data-hplan-type")
            || $r.find("td[data-hplan-type] span.badge").text()
            || ""
        ).trim();
        const planCode = ($r.find("td[data-plan-code]").text() || "").trim();
        const planDes = ($r.find("td[data-plan-des]").text() || "").trim();
        const qtyInput = $r.find("input[data-plan-qty]");
        const qty = (qtyInput.val() || qtyInput.attr("data-plan-qty") || "").trim();
        const $locationSelect = $r.find("select[data-location-code]");
        const location = ($locationSelect.find("option:selected").text()
            || $locationSelect.val()
            || $locationSelect.attr("data-location-code")
            || "").trim();
        const remarkInput = $r.find("input[data-remark]");
        const remark = (remarkInput.val() || remarkInput.attr("data-remark") || "").trim();
        const status = ($r.find("td>span[data-status]").text() || "").trim();
        const bloodType = ($r.find("select.nonmed-blood-type-select").val() || "").trim();
        const urgency = ($r.find("select.nonmed-urgency-select").val() || "").trim();

        if (!planDes && !planCode) {
            return;
        }

        nonMedList.push([
            hplanType,
            planCode,
            planDes,
            qty ? `qty:${qty}` : "",
            bloodType ? `bloodType:${bloodType}` : "",
            urgency ? `urgency:${urgency}` : "",
            location ? `location:${location}` : "",
            remark ? `remark:${remark}` : "",
            status ? `status:${status}` : ""
        ].filter(Boolean).join(", "));
    });

    return { nonMedList };
};
