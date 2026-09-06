/// <reference path="..\lib\jquery\dist\jquery.js" />

var oldMed = [];
var medFreq = [];
var medIndication = [];
var medPathWay = [];
var medItem = [];

function getRandomData_Med(x) {

    var index = Math.floor(Math.random() * x);

    var list = [
        { orderplaid: '', plancode: '1XYZAL', plandes: 'Xyzal', unitdose: 'Tab', plangen: ''},
        { orderplaid: '', plancode: '1SOMA', plandes: 'Soma', unitdose: 'Tab', plangen: '' },
        { orderplaid: '', plancode: '1MIYAR', plandes: 'Miyarisan', unitdose: 'Tab', plangen: '' },
        { orderplaid: '', plancode: '1TOLIZ', plandes: 'Tolizole', unitdose: 'Tab', plangen: '' },
        { orderplaid: '', plancode: '777777', plandes: 'MsdsdsdRI', unitdose: 'Tab', plangen: '' },
        { orderplaid: '', plancode: '33072B', plandes: 'CsdsdT', unitdose: 'Tab', plangen: '' }
    ];

    var newList = [];
    newList.push(list.at(index));

    return newList;
};





//初始化
function init_MedOrder() {
    //table
    var table = $("#medorder_table");
    //changeFlag重製
    changeObj.Med = false;

    var dfd_fn_GetMedicineItem = $.Deferred();
    var dfd_fn_GetMedPathWayData = $.Deferred();
    var dfd_fn_GetMedFreqData = $.Deferred();
    var dfd_fn_GetMedIndictionData = $.Deferred();
    $.when(
        //撈基本藥品資料
        fn_GetMedicineItem(dfd_fn_GetMedicineItem),
        fn_GetMedPathWayData(dfd_fn_GetMedPathWayData),
        fn_GetMedFreqData(dfd_fn_GetMedFreqData),
        fn_GetMedIndictionData(dfd_fn_GetMedIndictionData),
    ).done(function (
        r_fn_GetMedicineItem,
        r_fn_GetMedPathWayData,
        r_fn_GetMedFreqData,
        r_fn_GetMedIndictionData) {

        if (r_fn_GetMedicineItem.isSuccess == true &&
            r_fn_GetMedPathWayData.isSuccess == true &&
            r_fn_GetMedFreqData.isSuccess == true &&
            r_fn_GetMedIndictionData.isSuccess == true) {

            var trlist = table.find("tbody > tr:not(.medorder_tr_templete)")
            $.each(trlist, function (k, v) {

                var med_code = $(v).find('td[data-plan-code]').attr('data-plan-code');
                var med_des = $(v).find('td[data-plan-des]').attr('data-plan-des');
                var med_gen = $(v).find('td[data-plan-gen]').attr('data-plan-gen');
                var filterData = medItem.find(({ MedId }) => MedId == med_code);

                if (checkAutoOperation(filterData) == false || (med_des !== undefined && med_des == "Others" )) {
                    $(v).find('input[data-total-qty]').prop('readonly', false);
                    $(v).find("input[data-qty-daily]").prop('readonly', false);
                }

            });
        }


    });



    //顯示筆數
    fn_showMedOrderCount();
    //設定icheck屬性
    fn_setmedicheck(table);
    //置底
    /*    table.scrollTop(table.height());*/

    $('.add_medorders').click(function () {
        fn_addMedOrder(getRandomData_Med(4));
        fn_showMedOrderCount();
    });

    $('.delete_medorders').click(function () {
        fn_deleteMedOrders(table);
    });



    $("#MedPrint").click(function () {
        //取得tr
        var checklist = table.find("input:checked:not(.all):not(.medorder_tr_templete)")
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
        } else {

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


    //change event

    table.find('input[data-unit-dose-input]').change(function () {
        
        $(this).attr('data-unit-dose-input', $(this).val());
        $(this).closest('td').attr('data-unit-dose', $(this).val());
        fn_checkMedOrderChange($(this));
    });

    //table.find('td[data-unit-dose]').change(function () {
    //    $(this).attr('data-unit-dose', $(this).val());
    //    fn_checkMedOrderChange($(this));
    //});

    table.find('input[data-med-bag]').change(function () {
        $(this).attr('data-med-bag', $(this).val());
        fn_checkMedOrderChange($(this));
    });

    //次量
    table.find('input[data-qty-dose]').change(function () {
        $(this).attr('data-qty-dose', $(this).val());
        autoOperationQralDose($(this));
        fn_checkMedOrderChange($(this));
    });

    table.find('td > select[data-dose-path]').change(function () {
        $(this).attr('data-dose-path', $(this).val());
        fn_checkMedOrderChange($(this));
    });

    //頻次
    table.find('td > select[data-freq-code]').change(function () {
        $(this).attr('data-freq-code', $(this).val());
        autoOperationQralDose($(this));
        fn_checkMedOrderChange($(this));
    });

    table.find('td > select[data-dose-indication]').change(function () {
        $(this).attr('data-dose-indication', $(this).val());
        fn_checkMedOrderChange($(this));
    });

    //日量
    table.find('input[data-qty-daily]').change(function () {
        $(this).attr('data-qty-daily', $(this).val());
        fn_checkMedOrderChange($(this));
    });

    //天數
    table.find('input[data-plan-days]').change(function () {
        $(this).attr('data-plan-days', $(this).val());
        autoOperationQralDose($(this));
        fn_checkMedOrderChange($(this));
    });

    table.find('input[data-total-qty]').change(function () {
        $(this).attr('data-total-qty', $(this).val());
        fn_checkMedOrderChange($(this));
    });

    table.find('input[data-remark]').change(function () {

        $(this).attr('data-remark', $(this).val());
        fn_checkMedOrderChange($(this));
    });


    //enter event
    //次量
    table.find('input[data-qty-dose]').keyup(function (e) {
        if (e.keyCode == 13) {

            var result = checkInput($(this));
            if (!result) {
                return;
            }

            //fn_checkMedOrderChange($(this));

            var tt = $(this).closest('tr').next();
            if ($(this).closest('tr').next().length > 0) {
                $(this).closest('tr').next().find('input[data-qty-dose]').focus().select();
            } else {
                $(this).closest('tr').siblings('tr:visible:first').find('input[data-qty-dose]').focus().select();
            }
        }
    });

    table.find('input[data-total-qty]').keyup(function (e) {
        if (e.keyCode == 13) {

            var result = checkInput($(this));
            if (!result) {
                return;
            }


            var tt = $(this).closest('tr').next();
            if ($(this).closest('tr').next().length > 0) {
                $(this).closest('tr').next().find('input[data-total-qty]').focus().select();
            } else {
                $(this).closest('tr').siblings('tr:visible:first').find('input[data-total-qty]').focus().select();
            }
        }
    });

    table.find('input[data-plan-days]').keyup(function (e) {
        if (e.keyCode == 13) {

            var result = checkInput($(this));
            if (!result) {
                return;
            }


            var tt = $(this).closest('tr').next();
            if ($(this).closest('tr').next().length > 0) {
                $(this).closest('tr').next().find('input[data-plan-days]').focus().select();
            } else {
                $(this).closest('tr').siblings('tr:visible:first').find('input[data-plan-days]').focus().select();
            }
        }
    });

    table.find('input[data-qty-daily]').keyup(function (e) {
        if (e.keyCode == 13) {

            var result = checkInput($(this));
            if (!result) {
                return;
            }


            var tt = $(this).closest('tr').next();
            if ($(this).closest('tr').next().length > 0) {
                $(this).closest('tr').next().find('input[data-qty-daily]').focus().select();
            } else {
                $(this).closest('tr').siblings('tr:visible:first').find('input[data-qty-daily]').focus().select();
            }
        }
    });

    table.find('input[data-remark]').keyup(function (e) {
        if (e.keyCode == 13) {

            var tt = $(this).closest('tr').next();
            if ($(this).closest('tr').next().length > 0) {
                $(this).closest('tr').next().find('input[data-remark]').focus().select();
            } else {
                $(this).closest('tr').siblings('tr:visible:first').find('input[data-remark]').focus().select();
            }
        }
    });



    //click event
    table.find('input[data-unit-dose-input]').click(function (e) {
        $(this).focus().select();
    });

    table.find('input[data-qty-dose]').click(function (e) {
        $(this).focus().select();
    });

    table.find('input[data-total-qty]').click(function (e) {
        $(this).focus().select();
    });

    table.find('input[data-plan-days]').click(function (e) {
        $(this).focus().select();

    });

    table.find('input[data-qty-daily]').click(function (e) {
        $(this).focus().select();
    });

    table.find('input[data-remark]').click(function (e) {
        $(this).focus().select();
    });

    //
    table.find('input[data-qty-dose],input[data-total-qty],input[data-plan-days],input[data-qty-daily]').focusout(function (e) {
        checkInput($(this));
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

    $('#medorder_table').on("mouseenter", "td[data-plan-des]", function () {
        if (this.offsetWidth < this.scrollWidth) {
            $(this).attr('data-toggle', 'tooltip').attr('title', $(this).text());
        }
    });

    $('#medorder_table').on("mouseenter", "td[data-plan-gen]", function () {
        if (this.offsetWidth < this.scrollWidth) {
            $(this).attr('data-toggle', 'tooltip').attr('title', $(this).text());
        }
    });

    $('#medorder_table').on("mouseleave", "td[data-plan-des]", function () {
        $(this).attr('data-toggle', '');
    });

    $('#medorder_table').on("mouseleave", "td[data-plan-gen]", function () {
        $(this).attr('data-toggle', '');
    });

    $('#medorder_table').on('hidden.bs.collapse', function () {
        // do something...
        $('.med_collapse_btn').find('i').removeClass('fa-arrow-up').addClass('fa-arrow-down');
    })

    $('#medorder_table').on('show.bs.collapse', function () {
        // do something...
        $('.med_collapse_btn').find('i').removeClass('fa-arrow-down').addClass('fa-arrow-up');
    })


    fn_reload_old_medorder_data();

    fn_CheckBeforeUnload();
}

//顯示筆數
function fn_showMedOrderCount() {
    var dataCount = $("#medorder_table").find("tbody > tr:visible").length
    var strCount = (dataCount == 0) ? "0" : dataCount.toString();
    //顯示筆數
    $("#medorder_count").text(strCount);
}



function autoOperationQralDose(element) {

    var _element = $(element);
    var med_code = _element.closest('td').siblings('td[data-plan-code]').attr('data-plan-code');
    var filterData = medItem.find(({ MedId }) => MedId == med_code);

    if (checkAutoOperation(filterData)) {
        console.log('checkAutoOperation: true');

        var _tr_element = _element.closest('tr');
        var _td_med_freq = _tr_element.find('td > select[data-freq-code]').attr('data-freq-code');
        var _td_med_qty_dose = _tr_element.find('input[data-qty-dose]').attr('data-qty-dose');
        var _td_med_days = _tr_element.find("input[data-plan-days]").attr('data-plan-days');


        var med_frq_info = medFreq.find(({ FrqCode }) => FrqCode == _td_med_freq);


        console.log(parseFloat(_td_med_qty_dose));
        console.log(parseFloat(_td_med_days));

        if (med_frq_info.FrqForDays != 0 &&
            _td_med_freq != undefined &&
            (_td_med_qty_dose != undefined && parseFloat(_td_med_qty_dose) > 0) &&
            (_td_med_days != undefined && parseFloat(_td_med_days) > 0)) {
            console.log('FrqForDays:' + med_frq_info.FrqForDays);


            var totalDose = Math.round((parseFloat(_td_med_qty_dose) * med_frq_info.FrqOneDayTimes * parseFloat(_td_med_days) * med_frq_info.FrqForTimes) / med_frq_info.FrqForDays);
            var qtyDaily = parseFloat(_td_med_qty_dose) * med_frq_info.FrqOneDayTimes

            _tr_element.find('input[data-qty-daily]').attr('data-qty-daily', qtyDaily).val(qtyDaily);
            _tr_element.find('input[data-total-qty]').attr('data-total-qty', totalDose).val(totalDose);
            _tr_element.find('input[data-total-qty]').prop('readonly', true);
            console.log('totalDose: ' + totalDose);
            console.log('qtyDaily: ' + qtyDaily);

        } else {
            _tr_element.find('input[data-total-qty]').prop('readonly', false);
        }



    } else {
        console.log('checkAutoOperation: false');
    }

}





function checkAutoOperation(MedItemData) {
    if (!MedItemData) {
        return false;
    }
    if (MedItemData.MedType == '1' &&
        MedItemData.UnitSpec == MedItemData.PackSpec && MedItemData.UnitSpec != null && MedItemData.UnitSpec != undefined) {
        return true;
    } else {
        return false;
    }
}



//iCheck
function fn_setmedicheck(table) {
    // iCheck
    table.find("input").iCheck({
        labelHover: true,
        cursor: true,
        checkboxClass: "icheckbox_flat-pink",
        radioClass: "iradio_square-blue",
        increaseArea: "15%"
    });

    var checkAll = table.find('input.all:not(.medorder_tr_templete)');
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

//畫面add
function fn_addMedOrder(DataList) {
    if (DataList !== "undefined" && DataList.length > 0) {

        var table = $("#medorder_table")
        $.each(DataList, function (index, value) {
            var maxseq = table.find("tbody > tr:not(.medorder_tr_templete):last > td[data-seq-no]").attr("data-seq-no");
            if (maxseq == undefined) {
                maxseq = 1;
            } else {
                maxseq++;
            }

            var $el = $(".medorder_tr_templete:first")
                .clone(true, true)
                .removeClass("medorder_tr_templete")
                .attr('hidden', false)
                .attr("data-modify-type", "I");

            $el.find("input.medorder_tr_templete").removeClass("medorder_tr_templete");


            $el.find("td[data-orderplan-id]").attr('data-orderplan-id', value.orderplaid).text(value.orderplaid);
            $el.find("td[data-plan-code]").attr('data-plan-code', value.plancode).text(value.plancode);
            $el.find("td[data-plan-des]").attr('data-plan-des', value.plandes).text(value.plandes);
            $el.find("td[data-plan-gen]").attr('data-plan-gen', value.plangen).text(value.plangen);
            $el.find("td>span[data-status]").attr('data-status', "").addClass("badge-pink").text("?");
            $el.find("td[data-unit-dose]").attr('data-unit-dose', value.MedUnitSpec).text(value.MedUnitSpec);

            if (value.plandes == "Others") {
                var inputElement = $('<input>').attr({
                    type: 'text',
                    class: 'form-control form-control-sm text-center text-primary',
                    'data-unit-dose-input': '',
                    value: ''
                });

                $el.find("td[data-unit-dose]").append(inputElement);
                $el.find('input[data-unit-dose-input]').change(function () {

                    $(this).attr('data-unit-dose-input', $(this).val());
                    $(this).closest('td').attr('data-unit-dose', $(this).val());
                    fn_checkMedOrderChange($(this));
                });
            }

            //default
            $el.find("input[data-med-bag]").attr('data-med-bag', 1).val(1);
            $el.find("input[data-qty-dose]").attr('data-qty-dose', '').val('');
            $el.find("td > select[data-dose-path]").attr('data-dose-path', '').val('');
            $el.find("td > select[data-freq-code]").attr('data-freq-code', value.MedDefaultFreq).val(value.MedDefaultFreq);
            $el.find("td > select[data-dose-indication]").attr('data-dose-indication', '').val('');
            $el.find("input[data-qty-daily]").attr('data-qty-daily', '').val('');
            $el.find("input[data-plan-days]").attr('data-plan-days', 1).val(1);
            $el.find("input[data-total-qty]").attr('data-total-qty', '').val('');

            // Optional fields (e.g. history transfer)
            if (value.MedQtyDose != null && String(value.MedQtyDose).trim() !== '') {
                $el.find("input[data-qty-dose]").attr('data-qty-dose', value.MedQtyDose).val(value.MedQtyDose);
            }
            if (value.MedDosePath != null && String(value.MedDosePath).trim() !== '') {
                var $path = $el.find("td > select[data-dose-path]");
                $path.val(String(value.MedDosePath).trim());
            }
            if (value.MedPlanDays != null && String(value.MedPlanDays).trim() !== '') {
                $el.find("input[data-plan-days]").attr('data-plan-days', value.MedPlanDays).val(value.MedPlanDays);
            }
            if (value.MedTotalQty != null && String(value.MedTotalQty).trim() !== '') {
                $el.find("input[data-total-qty]").attr('data-total-qty', value.MedTotalQty).val(value.MedTotalQty);
            }
            if (value.MedRemark != null && String(value.MedRemark).trim() !== '') {
                $el.find("input[data-remark]").attr('data-remark', value.MedRemark).val(value.MedRemark);
            }

            //hidden
            $el.find("td[data-seq-no]").attr('data-seq-no', maxseq);
            console.log($el);



            var med_code = $el.find('td[data-plan-code]').attr('data-plan-code');
            var filterData = medItem.find(({ MedId }) => MedId == med_code);

            if (checkAutoOperation(filterData) == false || (value.plandes !== undefined && value.plandes == "Others")) {
                $el.find('input[data-total-qty]').prop('readonly', false);
                $el.find("input[data-qty-daily]").prop('readonly', false);
            }

            table.find("tbody").append($el);

        });

        changeObj.Med = true;
        fn_CheckBeforeUnload();
        fn_setmedicheck(table);
        fn_showMedOrderCount();
        table.scrollTop(table.height());
    }
}

//刪除
function fn_deleteMedOrders(table) {
    //取得tr
    var checklist = table.find("input:checked:not(.all)");

    if (checklist.length > 0) {
        checklist.each(function (idx, val) {

            var orderplaid = $(val).closest('tr').attr("data-orderplan-id");

            if (orderplaid == "") {
                $(val).closest('tr:not(.medorder_tr_templete)').remove();
            } else {
                $(val).closest('tr:not(.medorder_tr_templete)').attr("hidden", true).attr("data-modify-type", "D");
            }
        });

        changeObj.Med = true;
        fn_CheckBeforeUnload();
    }



    //取消checkbox
    table.find('input.all').iCheck('uncheck');
    //計數
    fn_showMedOrderCount();

}




function fn_GetMedIndictionData(dfd) {
    var vURL = "/HisOrder/Medicine/GetMedFreqData";
    var vSuccessFunc = function (msg) {

        var result = JSON.parse(msg);
        if (result.isSuccess == true) {
            var data = JSON.parse(result.returnValue);
            if (data != null && data != undefined) {
                medIndication.length = 0;
                $.each(data, function (k, v) {
                    medIndication.push(v);
                });
            }

            return dfd.resolve(result);
        }
    };
    var vErrorFunc = function () {

        return this;
    };

    ajaxGet(vURL, null, vSuccessFunc, vErrorFunc);

    return dfd.promise();;
}




function fn_GetMedPathWayData(dfd) {
    var vURL = "/HisOrder/Medicine/GetMedPathWayData";
    var vSuccessFunc = function (msg) {

        var result = JSON.parse(msg);
        if (result.isSuccess == true) {
            var data = JSON.parse(result.returnValue);
            if (data != null && data != undefined) {
                medPathWay.length = 0;
                $.each(data, function (k, v) {
                    medPathWay.push(v);
                });
            }

            return dfd.resolve(result);
        }
    };
    var vErrorFunc = function () {

        return this;
    };

    ajaxGet(vURL, null, vSuccessFunc, vErrorFunc);

    return dfd.promise();;
}

function fn_GetMedFreqData(dfd) {
    var vURL = "/HisOrder/Medicine/GetMedFreqData";
    var vSuccessFunc = function (msg) {

        var result = JSON.parse(msg);
        if (result.isSuccess == true) {
            var data = JSON.parse(result.returnValue);
            if (data != null && data != undefined) {
                medFreq.length = 0;
                $.each(data, function (k, v) {
                    medFreq.push(v);
                });
            }

            return dfd.resolve(result);
        }
    };
    var vErrorFunc = function () {

        return this;
    };

    ajaxGet(vURL, null, vSuccessFunc, vErrorFunc);

    return dfd.promise();;
}

function fn_GetMedicineItem(dfd) {
    var vURL = "/HisOrder/Medicine/GetMedicineItem";
    var vSuccessFunc = function (msg) {

        var result = JSON.parse(msg);
        if (result.isSuccess == true) {
            var data = JSON.parse(result.returnValue);
            if (data != null && data != undefined) {
                medItem.length = 0;
                $.each(data, function (k, v) {
                    medItem.push(v);
                });
            }

            return dfd.resolve(result);
        }
    };
    var vErrorFunc = function () {

        return this;
    };

    ajaxGet(vURL, null, vSuccessFunc, vErrorFunc);

    return dfd.promise();
}


//reload old data
function fn_reload_old_medorder_data() {

    var inhospid = $('#patientInhospid').val();
    var vURL = "/HisOrder/Medicine/GetHisOrderPlan";
    var vData = {
        inhospid: inhospid
    };
    var vSuccessFunc = function (msg) {

        var result = JSON.parse(msg);
        if (result.isSuccess == true) {
            var data = JSON.parse(result.returnValue);
            if (data != null && data != undefined) {
                oldMed.length = 0;
                $.each(data, function (k, v) {
                    oldMed.push(v);
                });
            }
        }
    };
    var vErrorFunc = function () {

        return this;
    };
    ajaxGet(vURL, vData, vSuccessFunc, vErrorFunc);

}


// reload partial view 
function fn_reload_medorder_partial_view() {

    var medorder_section = $(".medorder-section");
    medorder_section.empty();
    var vURL = "/HisOrder/Medicine/ReloadPartialView";
    var vSuccessFunc = function (result) {
        /*layer.load();*/
        medorder_section.html(result);
        //重新綁定事件
        init_MedOrder();
        /*layer.closeAll('loading');*/
    };
    var vErrorFunc = function () {
        medorder_section.html("載入失敗");
        return this;
    };
    ajaxGet(vURL, null, vSuccessFunc, vErrorFunc);

}



//取得前端資訊，將醫令寫入DB
function fn_SaveMedOrderByElements(dfd1, status) {

    //var dfd = $.Deferred();

    var trList = $("#medorder_table").find("tbody > tr:not(.medorder_tr_templete)");
    const orderAry = [];
    trList.each(function (idx, val) {

        var modifyType = $(val).attr("data-modify-type");
        var obj = {
            Orderplanid: $(val).attr("data-orderplan-id"),
            PlanCode: $(val).find("td[data-plan-code]").attr("data-plan-code"),
            PlanDes: $(val).find("td[data-plan-des]").text(),
            PlanGen: $(val).find("td[data-plan-gen]").text(),
            UnitDose: $(val).find("td[data-unit-dose]").attr("data-unit-dose"),
            MedBag: $(val).find("input[data-med-bag]").attr("data-med-bag"),
            QtyDose: $(val).find("input[data-qty-dose]").attr("data-qty-dose"),
            DosePath: $(val).find("td > select[data-dose-path]").attr("data-dose-path"),
            FreqCode: $(val).find("td > select[data-freq-code]").attr("data-freq-code"),
            DoseIndication: $(val).find("td > select[data-dose-indication]").attr("data-dose-indication"),
            QtyDaily: $(val).find("input[data-qty-daily]").attr("data-qty-daily"),
            PlanDays: $(val).find("input[data-plan-days]").attr("data-plan-days"),
            TotalQty: $(val).find("input[data-total-qty]").attr("data-total-qty"),
            SeqNo: $(val).find("td[data-seq-no]").attr("data-seq-no"),
            ModifyType: modifyType,
            Remark: $(val).find("input[data-remark]").attr("data-remark")
        };
        orderAry.push(obj);
    });

    var vURL = "/HisOrder/Medicine/ModifyMedOrder";
    var vData = {
        inOrder: JSON.stringify(orderAry),
        inStatus: status
    };
    var vSuccessFunc = function (msg) {

        var objResult = JSON.parse(msg);
        console.log('med ok');
        dfd1.resolve(objResult);
    };
    var vErrorFunc = function (xhr) {

        return xhr;
    };

    ajax(vURL, vData, vSuccessFunc, vErrorFunc);


    return dfd1.promise();
}


function fn_checkMedOrderChange(element) {

    var changeFlag = false;

    var $tr = $(element).closest('tr');

    var oldData = oldMed.find(({ Orderplanid }) => Orderplanid == $tr.attr('data-orderplan-id'));

    var newOrder = {
        Orderplanid: $tr.attr("data-orderplan-id"),
        PlanCode: $tr.find("td[data-plan-code]").text(),
        PlanDes: $tr.find("td[data-plan-des]").text(),
        PlanGen: $tr.find("td[data-plan-gen]").text(),
        UnitDose: $tr.find("td[data-unit-dose]").attr("data-unit-dose"),
        MedBag: $tr.find("input[data-med-bag]").attr("data-med-bag"),
        QtyDose: $tr.find("input[data-qty-dose]").attr("data-qty-dose"),
        DosePath: $tr.find("td > select[data-dose-path]").attr("data-dose-path"),
        FreqCode: $tr.find("td > select[data-freq-code]").attr("data-freq-code"),
        DoseIndication: $tr.find("td > select[data-dose-indication]").attr("data-dose-indication"),
        QtyDaily: $tr.find("input[data-qty-daily]").attr("data-qty-daily"),
        PlanDays: $tr.find("input[data-plan-days]").attr("data-plan-days"),
        TotalQty: $tr.find("input[data-total-qty]").attr("data-total-qty"),
        Remark: $tr.find("input[data-remark]").attr('data-remark'),
    };

    //欄位是否異動-擴充
    if (oldData != undefined &&
        ((oldData.UnitDose != newOrder.UnitDose) ||
            (oldData.MedBag != newOrder.MedBag) ||
            (oldData.QtyDose != newOrder.QtyDose) ||
            (oldData.DosePath != newOrder.DosePath) ||
            (oldData.FreqCode != newOrder.FreqCode) ||
            (oldData.DoseIndication != newOrder.DoseIndication) ||
            (oldData.QtyDaily != newOrder.QtyDaily) ||
            (oldData.PlanDays != newOrder.PlanDays) ||
            (oldData.TotalQty != newOrder.TotalQty) ||
            (oldData.Remark != newOrder.Remark))
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

    changeObj.Med = changeFlag;
    fn_CheckBeforeUnload();

    return changeFlag;

}
