
// 中文：需在使用的 PartialView 中宣告全域變數
// 英文：Declare variables in the parent view
// var HISORDER_ROOT = Root = '@Url.Content("~/HisOrder/")';
function fn_RePrint(inOrderplanids, inHospID) {

    // inOrderplanids = [];
    // inOrderplanids.push("1541","1542","1551", "1552");

    let strOrderplanids = '';

    if (inOrderplanids != null) {

        strOrderplanids = inOrderplanids.join(',');
    }

    $.ajax({
        type: 'POST',
        url: HISORDER_ROOT + "Print/PrintForm",
        data: {
            RePrint: 'Y',
            inOrderplanids: strOrderplanids,
            inHospID: inHospID,
        },
        async: false,
        dataType: 'text',
        success: function (result) {
            $('#PrintForm').html(result);
            $('#PrintMenu').modal('show');
        },
        error: function (xhr, ajaxOptions, thrownError) { },
        complete: function (XMLHttpRequest, textStatus) { }
    });

    //window.open(Root + "Print/PrintForm?RePrint=Y&inOrderplanids=" + strOrderplanids);
}