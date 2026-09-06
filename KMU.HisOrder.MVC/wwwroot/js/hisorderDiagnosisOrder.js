
var Diagnosisinput = document.querySelector('input[name=Diagnosis-drag-sort]');
var tagify = null;
if (Diagnosisinput) {
    tagify = new Tagify(Diagnosisinput, {
        userInput: false,
    });
}
window.tagify = tagify;

function init_Diagnosis() {
    if (!tagify) {
        return;
    }
    reloadOrderDiagnosis();
}

function reloadOrderDiagnosis() {
    if (!tagify) {
        return;
    }

    tagify.removeAllTags();

    var ResultOrderData = [];

    $.ajax({
        type: 'POST',
        url: Root + "Diagnosis/getICDHisorderplanData",
        data: {},
        async: false,
        dataType: 'json',
        success: function (result) {
            ResultOrderData = result;
        },
        error: function (xhr, ajaxOptions, thrownError) { },
        complete: function (XMLHttpRequest, textStatus) { }
    });

    var IcdItemList = [];

    if (ResultOrderData != null) {
        for (var i = 0; i < ResultOrderData.length; i++) {

            var MedicalItem = {
                orderplanid: ResultOrderData[i].orderplanid,
                plancode: ResultOrderData[i].planCode,
                plandes: ResultOrderData[i].planDes,
                qty: 1,
            };

            IcdItemList.push(MedicalItem);
        }
    }

    fn_addIcdOrder(IcdItemList);
}

function fn_addIcdOrder(DataList) {
    if (!tagify) {
        return;
    }

    if (DataList !== 'undefined' && DataList.length > 0) {
        for (var i = 0; i < DataList.length; i++) {
            var TagName = DataList[i].plancode + ' ' + DataList[i].plandes;
            tagify.addTags([{ value: TagName, orderplanid: DataList[i].orderplanid, plancode: DataList[i].plancode, plandes: DataList[i].plandes }]);
        }
    }
}

/* 儲存診斷前的檢核 */
function DiagnosisCheckBeforeSend() {
    if (!tagify) {
        return false;
    }
    var DiagnosisOrderlist = tagify.value;

    if (DiagnosisOrderlist.length == 0) {
        layer.alert("Order need at least one Diagnosis.", { icon: 2, title: "Error" });
        return false;
    }
}

function fn_SaveDiagnosis(dfd1, status) {
    if (!tagify) {
        dfd1.resolve({ isSuccess: false, Message: 'Diagnosis UI not available' });
        return dfd1.promise();
    }
    var DiagnosisOrderlist = tagify.value;

    var Orderlist = [];

    if (DiagnosisOrderlist.length > 0) {

        for (var i = 0; i < DiagnosisOrderlist.length; i++) {
            var obj = {
                Orderplanid: DiagnosisOrderlist[i].orderplanid,
                PlanCode: DiagnosisOrderlist[i].plancode,
                PlanDes: DiagnosisOrderlist[i].plandes,
                SeqNo: i,
            };
            Orderlist.push(obj);
        }
    }

    var DiagnosisResult = {};

    $.ajax({
        type: 'POST',
        url: Root + "Diagnosis/ModifyICDOrder",
        data: {
            inOrder: Orderlist,
            inStatus: status
        },
        async: false,
        dataType: 'json',
        success: function (result) {
            DiagnosisResult = result;
        },
        error: function (xhr, ajaxOptions, thrownError) { },
        complete: function (XMLHttpRequest, textStatus) { }
    });

    reloadOrderDiagnosis();
    dfd1.resolve(DiagnosisResult);

    return dfd1.promise();
}

// Dragdown update DOM
function onDragEnd(elm) {
    if (tagify) {
        tagify.updateValueByDOMTags();
    }
}

$(document).ready(function () {

    init_Diagnosis();

    if (!tagify || !tagify.DOM) {
        return;
    }

    var dragsort = new DragSort(tagify.DOM.scope, {
        selector: '.' + tagify.settings.classNames.tag,
        callbacks: {
            dragEnd: onDragEnd
        }
    });
})
