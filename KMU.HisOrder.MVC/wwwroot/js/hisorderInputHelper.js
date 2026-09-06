
// 2023.06.19  update async false -> true , update by 1100079.
function RenderIcdMenu() {

    $.ajax({
        type: 'POST',
        url: Root + "InputHelper/getICDMenu",
        data: {},
        async: true,
        dataType: 'Text',
        beforeSend: function () {
            $('#ICD').append(
                "<div id='ICDloadingGif' class='MenuLoadingContainer'>" +
                "<img src='../../images/SpinnerLoading.gif' class='MenuLoading' alt='loading...'>" +
                "</div>"
            );
        },

        success: function (result) {
            $('#ICD').html(result);

            // Auto Selected ICD Row 0
            $('#Categoryul > li').not('.headli').eq(0).click();
        },
        error: function (xhr, ajaxOptions, thrownError) {
            console.log(thrownError);
        },
        complete: function (XMLHttpRequest, textStatus) {
            $('#ICDloadingGif').remove();
        }
    });
}

// 2023.06.19  update async false -> true , update by 1100079.
function RenderMedMenu() {
    $.ajax({
        type: 'POST',
        url: Root + "InputHelper/getMedMenu",
        data: {},
        async: true,
        dataType: 'Text',
        beforeSend: function () {
            $('#Med').append("<div id='MedloadingGif' class='MenuLoadingContainer'><img src='../../images/SpinnerLoading.gif' class='MenuLoading' alt='loading...'></div>");
        },
        success: function (result) {
            $('#Med').html(result);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            console.log(thrownError);
        },
        complete: function (XMLHttpRequest, textStatus) {
            $('#MedloadingGif').remove();
        }
    });
}

// 2023.06.19  update async false -> true , update by 1100079.
function RenderLabMenu() {
    $.ajax({
        type: 'POST',
        url: Root + "InputHelper/getNonMedMenu",
        data: { inNonMedType: 'Lab' },
        async: true,
        dataType: 'Text',
        beforeSend: function () {
            $('#Lab').append("<div id='LabloadingGif' class='MenuLoadingContainer'><img src='../../images/SpinnerLoading.gif' class='MenuLoading' alt='loading...'></div>");
        },
        success: function (result) {
            $('#Lab').html(result);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            console.log(thrownError);
        },
        complete: function (XMLHttpRequest, textStatus) {
            $('#LabloadingGif').remove();
        }
    });
}

// 2023.06.19  update async false -> true , update by 1100079.
function RenderExamMenu() {
    $.ajax({
        type: 'POST',
        url: Root + "InputHelper/getNonMedMenu",
        data: { inNonMedType: 'Exam' },
        async: true,
        dataType: 'Text',
        beforeSend: function () {
            $('#Exam').append("<div id='ExamloadingGif' class='MenuLoadingContainer'><img src='../../images/SpinnerLoading.gif' class='MenuLoading' alt='loading...'></div>");
        },
        success: function (result) {
            $('#Exam').html(result);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            console.log(thrownError);
        },
        complete: function (XMLHttpRequest, textStatus) {
            $('#ExamloadingGif').remove();
        }
    });
}

// 2023.06.19  update async false -> true , update by 1100079.
function RenderPathMenu() {
    $.ajax({
        type: 'POST',
        url: Root + "InputHelper/getNonMedMenu",
        data: { inNonMedType: 'Path' },
        async: true,
        dataType: 'Text',
        beforeSend: function () {
            $('#Path').append("<div id='PathloadingGif' class='MenuLoadingContainer'><img src='../../images/SpinnerLoading.gif' class='MenuLoading' alt='loading...'></div>");
        },
        success: function (result) {
            $('#Path').html(result);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            console.log(thrownError);
        },
        complete: function (XMLHttpRequest, textStatus) {
            $('#PathloadingGif').remove();
        }
    });
}

// 2023.06.19  update async false -> true , update by 1100079.
function RenderBloodMenu() {
    $.ajax({
        type: 'POST',
        url: Root + "InputHelper/getNonMedMenu",
        data: { inNonMedType: 'Blood' },
        async: true,
        dataType: 'Text',
        beforeSend: function () {
            $('#Blood').append("<div id='BloodloadingGif' class='MenuLoadingContainer'><img src='../../images/SpinnerLoading.gif' class='MenuLoading' alt='loading...'></div>");
        },
        success: function (result) {
            $('#Blood').html(result);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            console.log(thrownError);
        },
        complete: function (XMLHttpRequest, textStatus) {
            $('#BloodloadingGif').remove();
        }
    });
}


// 2023.06.19  update async false -> true , update by 1100079.
function RenderSupplyMenu() {
    $.ajax({
        type: 'POST',
        url: Root + "InputHelper/getNonMedMenu",
        data: { inNonMedType: 'Supply' },
        async: true,
        dataType: 'Text',
        beforeSend: function () {
            $('#Supply').append("<div id='SupplyloadingGif' class='MenuLoadingContainer'><img src='../../images/SpinnerLoading.gif' class='MenuLoading' alt='loading...'></div>");
        },
        success: function (result) {
            $('#Supply').html(result);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            console.log(thrownError);
        },
        complete: function (XMLHttpRequest, textStatus) {
            $('#SupplyloadingGif').remove();
        }
    });
}

function getIcdChildNodes(inKey, inSearchMode, inContainer, inAsync) {

    $.ajax({
        type: 'POST',
        url: Root + "InputHelper/getChildIcdNodes",
        data: {
            inKey: inKey,
            ShowMode: inSearchMode,
        },
        async: inAsync,
        dataType: 'text',
        beforeSend: function () {
            // Child Loading
            // layer.load(0, { shade: [0.6, '#4A4A4A'] }); //0代表加载的风格，支持0-2
            $('.Loadingli').remove(); // Avoid Loading Icon repeat
            $(inContainer).append('<ul class="Loadingli" ><li><img src="../../images/loading01.gif"/></li></ul>')
        },
        success: function (result) {
            if (inSearchMode == 'Search') {
                $('.SearchNode').remove(); // Clear Search Result
            }
            $(inContainer).append(result);
        },
        error: function (xhr, ajaxOptions, thrownError) { },
        complete: function (XMLHttpRequest, textStatus) {
            // remove Loading
            // layer.close(layer.index);
            $('.Loadingli').remove();
        }
    });
}

function getIcdItemList(inContainer) {

    let Recordli = inContainer;
    let IcdCode = inContainer.find('.IcdCode').text();

    // if exists hidden nodes,just show the nodes
    let isNewNodes = true;
    $('#CategoryitemCol > .MenuNode').each(function () {
        $(this).addClass('CategorySwitchHidden');
        let ParentCode = $(this).data('parentcode');
        if (ParentCode == IcdCode) {
            $(this).removeClass('CategorySwitchHidden');
            isNewNodes = false;
        }
    })

    // Not exists nodes,get nodes from db
    if (isNewNodes == true) {
        getIcdChildNodes(IcdCode, "Menu", '#CategoryitemCol', false);
    }

    // Selected Row Style Change
    Recordli.addClass('SelectedNoteRecord');
    $('#Categoryul li').not(Recordli).not('#SearchICDCategory').removeClass('SelectedNoteRecord');
}

function ExpandNodes(event, inBtn) {

    let Recordli = $(inBtn).parent().parent();
    let IcdCode = Recordli.find('.IcdCode').text();
    let Otherli = $('#CategoryitemCol .Categoryitemul li').not('.headli').not(Recordli);

    // First expand get db data
    // Collapse close, hidden the expand row
    // Second expand show the hidden row

    // if have been exists the records show records else get the child records.
    if (Recordli.find('ul').length > 0) {
        Recordli.find('ul').removeClass('CollapseCloseHidden');
    } else {
        getIcdChildNodes(IcdCode, "Menu", Recordli, true);
    }

    // if have been expanded,collapse hidden the records.
    if (Recordli.find('.CollapseIconExpand').length > 0) {

        // Remove selected row BackColor
        Recordli.removeClass('SelectedCollapseBlock');

        // hidden the child data
        Recordli.find('ul').addClass('CollapseCloseHidden');

        // Selected collapse btn icon change
        Recordli.find('.CollapseIcon').removeClass('CollapseIconExpand');
    } else {
        // Selected Row Color Style Change
        Recordli.addClass('SelectedCollapseBlock');
        Otherli.removeClass('SelectedCollapseBlock');

        // Hidden Expand Row
        Otherli.find('ul').addClass('CollapseCloseHidden');

        // Selected collapse btn icon change
        Recordli.find('.CollapseIcon').addClass('CollapseIconExpand');
        Otherli.find('.CollapseIcon').removeClass('CollapseIconExpand');
    }
}

function SetMedicalItemAddRow(inItemCode, inItemName, inItemType, inMedObj, inNonMedObj) {

    try {
        let ItemColor = '';
        switch (inItemType) {
            case 'ICD':
                ItemColor = 'background-color: #DCEDFF;';
                break;
            case 'Med':
                ItemColor = 'background-color: #FFE2E5;';
                break;
            case 'NonMed':
                switch (inNonMedObj.ItemType) {
                    case "Lab":
                        ItemColor = 'background-color: #ded6ff;';
                        break;
                    case "Exam":
                        ItemColor = 'background-color: #b5e8ff;';
                        break;
                    case "Path":
                        ItemColor = 'background-color: #e7e7e7;';
                        break;
                    case "Blood":
                        ItemColor = 'background-color: #fac7c3;';
                        break;
                    default:
                        ItemColor = 'background-color: #FFF4D2;';
                        break;
                }
                break;
        }

        let Htmltr = '<tr class="AddData" style="' + ItemColor + '" title="' + inItemCode + "  " + inItemName + '">' +
            '<td><a href="#" class="RemoveBtn" onclick="RemoveItem(this)">&times;</button></td>' +
            '<td class="Code">' + inItemCode + '</td>' +
            '<td class="Name">' + inItemName + '</td>' +
            '<td class="Type" style="display:none;">' + inItemType + '</td>';

        if (inMedObj != null) {
            Htmltr += '<td class="MedType" style="display:none;">' + inMedObj.MedType + '</td>' +
                '<td class="MedGenericName">' + inMedObj.GenericName + '</td>' +
                '<td class="MedUnitSpec" style="display:none;">' + inMedObj.UnitSpec + '</td>' +
                '<td class="MedPackSpec" style="display:none;">' + inMedObj.PackSpec + '</td>' +
                '<td class="MedDefaultFreq" style="display:none;">' + inMedObj.DefaultFreq + '</td>' +
                '<td class="MedRefDuration" style="display:none;">' + inMedObj.RefDuration + '</td>' +
                '<td class="MedRemarks" style="display:none;">' + inMedObj.Remarks + '</td>';
        }

        if (inNonMedObj != null) {
            // NonMedColumn
            Htmltr += '<td class="NonMedItemType" style="display:none;">' + inNonMedObj.ItemType + '</td>' +
                '<td class="NonMedItemSpec" style="display:none;">' + inNonMedObj.ItemSpec + '</td>' +
                '<td class="NonMedRemark" style="display:none;">' + inNonMedObj.Remark + '</td>';
        }
        Htmltr += '</tr>';



        $('#BringOrderTable > tbody').append(Htmltr);

        document.getElementById('BringOrderColumn').scrollTop = document.getElementById('BringOrderColumn').scrollHeight - document.getElementById('BringOrderColumn').clientHeight;

        layer.msg('Add Success', { icon: "1", offset: "rb" });
    } catch (ex) {
        layer.msg('Add Error：\n' + ex, { icon: "2", offset: "rb" });
    }
}

function AddIcdData(event, inli) {

    let IcdCode = $(inli).find('.IcdCode').first().text();
    let IcdEnglishName = $(inli).find('.IcdEnglishName').first().text();

    let isRepeat = CheckRepeatAdd(IcdCode, 'ICD');
    if (isRepeat == false) {
        SetMedicalItemAddRow(IcdCode, IcdEnglishName, 'ICD', null, null);
    }

    // Cancel Event Bubbling
    event.stopPropagation();
}

function AddMedData(event, intr) {

    let inItemCode = $(intr).find('.MedId').first().text();
    let inItemName = $(intr).find('.GenericName').first().text();

    let inMedObj = {
        MedType: $(intr).find('.MedType').first().text(),
        GenericName: $(intr).find('.BrandName').first().text(),
        UnitSpec: $(intr).find('.UnitSpec').first().text(),
        PackSpec: $(intr).find('.PackSpec').first().text(),
        DefaultFreq: $(intr).find('.DefaultFreq').first().text(),
        RefDuration: $(intr).find('.RefDuration').first().text(),
        Remarks: $(intr).find('.Remarks').first().text(),
    };

    let isRepeat = CheckRepeatAdd(inItemCode, 'Med');
    if (isRepeat == false) {
        SetMedicalItemAddRow(inItemCode, inItemName, 'Med', inMedObj, null);
    }
}

function AddNonMedData(event, inSpan, intr) {

    let CurrentTarget = inSpan == null ? intr : inSpan;
    let CommentRow = $(CurrentTarget).find('.CommentRow').first().text();

    if (CommentRow) { return false; } // Comment row can't add to order.


    let inItemCode = $(CurrentTarget).find('.ItemId').first().text();
    let inItemName = $(CurrentTarget).find('.ItemName').first().text();

    let inNonMedObj = {
        ItemSpec: $(CurrentTarget).find('.ItemSpec').first().text(),
        ItemType: $(CurrentTarget).find('.ItemType').first().text(),
        Remark: $(CurrentTarget).find('.Remark').first().text(),
    };

    let isRepeat = CheckRepeatAdd(inItemCode, 'NonMed');
    if (isRepeat == false) {
        SetMedicalItemAddRow(inItemCode, inItemName, 'NonMed', null, inNonMedObj);
    }
}

function CheckRepeatAdd(inCode, inType) {

    let isRepeat = false;

    $('#BringOrderTable > tbody > tr').each(function () {
        if ($(this).find('.Type').text().trim() == inType.trim() &&
            $(this).find('.Code').text().trim() == inCode.trim()) {

            isRepeat = true;
        }
    })

    if (isRepeat == true) {
        layer.msg('Add Error：\n This item already exists and cannot be added again.', { icon: "2", offset: "rb" });
    }

    return isRepeat;
}

function RemoveItem(ina) {
    let Itemtr = $(ina).parent().parent();
    $(Itemtr).remove();
}

function ClearItem(isConfirm) {

    if (isConfirm == true) {
        layer.confirm('Do you want to clear all Item ?', {
            btn: ['Yes', 'No'] //按钮
        }, function () {
            $('#BringOrderTable tbody tr').remove();
            layer.close(layer.index);
        }, function () { });
    } else {
        $('#BringOrderTable tbody tr').remove();
        layer.close(layer.index);
    }
}
function BringBackOrder() {
    try {
        // Create global arrays.
        window.IcdItemList = [];
        window.MedItemList = [];
        window.NonMedItemList = [];

        $('#BringOrderTable > tbody > tr').each(function () {
            let $OrderplanId = '-1';
            let $PlanCode = $(this).find('.Code').text().trim();
            let $PlanDes = $(this).find('.Name').text().trim();
            let $PlanGen = $(this).find('.MedGenericName').text().trim();
            let $ItemType = $(this).find('.Type').text().trim();
            let $Qty = 1;

            let $MedType = $(this).find('.MedType').text().trim();
            let $MedGenericName = $(this).find('.MedGenericName').text().trim();
            let $MedUnitSpec = $(this).find('.MedUnitSpec').text().trim();
            let $MedPackSpec = $(this).find('.MedPackSpec').text().trim();
            let $MedDefaultFreq = $(this).find('.MedDefaultFreq').text().trim();
            let $MedRefDuration = $(this).find('.MedRefDuration').text().trim();
            let $MedRemarks = $(this).find('.MedRemarks').text().trim();

            let $NonMedItemType = $(this).find('.NonMedItemType').text().trim();
            let $NonMedItemSpec = $(this).find('.NonMedItemSpec').text().trim();
            let $NonMedRemark = $(this).find('.NonMedRemark').text().trim();

            let MedicalItem = {
                orderplanid: $OrderplanId,
                plancode: $PlanCode,
                plandes: $PlanDes,
                plangen: $PlanGen,
                MedType: $MedType,
                MedGenericName: $MedGenericName,
                MedUnitSpec: $MedUnitSpec,
                MedPackSpec: $MedPackSpec,
                MedDefaultFreq: $MedDefaultFreq,
                MedRefDuration: $MedRefDuration,
                MedRemarks: $MedRemarks,
                NonMedItemType: $NonMedItemType,
                NonMedItemSpec: $NonMedItemSpec,
                NonMedRemark: $NonMedRemark,
                qty: $Qty,
            };
           // console.log("Selected Order Plan: ", MedicalItem);
            switch ($ItemType) {
                case 'ICD':
                    window.IcdItemList.push(MedicalItem);
                    break;
                case 'Med':
                    window.MedItemList.push(MedicalItem);
                    break;
                case 'NonMed':
                    window.NonMedItemList.push(MedicalItem);
                    break;
            }
        });
       // console.log('ICD Items:', window.IcdItemList);

        // Use the global arrays in your order functions.
        fn_addIcdOrder(window.IcdItemList);
        fn_addMedOrder(window.MedItemList);
        fn_addNonMedorder(window.NonMedItemList);

        // Clear Menu Add Table Item
        $('#BringOrderTable > tbody > tr').remove();
        // success
        layer.msg('Successfully Added.', { icon: "1", offset: "rb" });
        // Modal Close
        $('#MedicalCategoryList').modal('hide');
    } catch (ex) {
        layer.alert("Bring back to order failed?<br />" + ex, { icon: 2, title: "Error" });
    }
}


// google js debounce (skip repeat execution function)
function debounce(func, delay) {
    var func = func;
    var timer;
    if (delay === undefined) {
        delay = 500;
    }
    return function () {
        var self = this, args = arguments;
        if (timer) {
            clearTimeout(timer);
            timer = null;
        }
        timer = setTimeout(function () {
            func.apply(self, args);
        }, delay);
    };
}

$(document).on('click', '#Categoryul > li:not(.headli):not(#SearchICDCategory)', function (event) {
    event.currentTarget;
    getIcdItemList($(this));
});

//// Scroll to the bottom 5px,Show the next 100 IcdNodes.
//$(window).on('scroll', '#CategoryitemCol', function () {
//    let viewH = $(this).height(); //可見高度
//    let contentH = $(this).get(0).scrollHeight; //内容高度
//    let scrollTop = $(this).scrollTop(); //滾動高度

//    //到達底部 20% 時,往下長100筆內容
//    if (contentH - viewH - scrollTop <= contentH * 0.2) {

//        let ShowCount = 0;
//        $('.SearchNode,.MenuNode > li').each(function () {
//            if ($(this).css('display') == 'none') {
//                $(this).show();
//                ShowCount++;
//            }

//            if (ShowCount == 100) { return false; }
//        });
//        ShowCount = 0;
//    }
//});

$(document).ready(function () {
    RenderIcdMenu();
    RenderMedMenu();
    RenderLabMenu();
    RenderExamMenu();
    RenderPathMenu();
    RenderSupplyMenu();
    RenderBloodMenu();

    //// Left ICD Menu Click
    //$('#Categoryul > li').not('.headli').not('#SearchICDCategory').on('click', function (event) {
    //    event.currentTarget;
    //    getIcdItemList($(this));
    //});

    // Auto Selected ICD Row 0
    // $('#Categoryul > li').not('.headli').eq(0).click();

    // Scroll to the bottom 5px,Show the next 100 IcdNodes.
    $('#CategoryitemCol').on('scroll', function () {
        let viewH = $(this).height(); //可見高度
        let contentH = $(this).get(0).scrollHeight; //内容高度
        let scrollTop = $(this).scrollTop(); //滾動高度

        //到達底部 20% 時,往下長100筆內容
        if (contentH - viewH - scrollTop <= contentH * 0.2) {

            let ShowCount = 0;
            $('.SearchNode,.MenuNode > li').each(function () {
                if ($(this).css('display') == 'none') {
                    $(this).show();
                    ShowCount++;
                }

                if (ShowCount == 100) { return false; }
            });
            ShowCount = 0;
        }
    });

    // Search ICD
    var CurrentShowParentCode = "";
    var CurrentKey = "";
    $('#ICDMenuSearch').on("keyup", debounce(function () {

        let inKey = $('#ICDMenuSearch').val();

        if (CurrentKey.trim() == inKey.trim()) { return false; } // Prevent repeated triggering

        $('.SearchNode').remove(); // Clear Search Result

        CurrentKey = inKey;

        // No any key,clear the result
        if (inKey.length < 3) {
            // Category
            $('#SearchICDCategory').hide();
            $('#CategoryCol > #Categoryul > li').not('.headli').not('#SearchICDCategory').each(function () {
                $(this).show();
            });

            // CategoryItem ( Only display the original show ul )
            $('#CategoryitemCol > .MenuNode').each(function () {
                if ($(this).data('parentcode') == CurrentShowParentCode) {
                    $(this).removeClass('CategorySwitchHidden');
                }
            })

            return false;
        }

        // Have search key
        // Category
        $('#SearchICDCategory').show();
        $('#CategoryCol > #Categoryul > li').not('.headli').not('#SearchICDCategory').each(function () {
            $(this).hide();
        });

        // CategoryItem
        $('#CategoryitemCol > .MenuNode').each(function () {
            if ($(this).attr('class').includes('CategorySwitchHidden') == false) {
                CurrentShowParentCode = $(this).data('parentcode');
            }
            $(this).addClass('CategorySwitchHidden');
        })

        getIcdChildNodes(inKey, "Search", '#CategoryitemCol', true); // ajax get the search result

    }, 500));

    // Search Med
    $('#MedMenuSearch').bind("keyup", function () {

        let inKey = $(this).val();

        if (inKey == false) {
            $('#MedMenuTable > tbody > tr').each(function () {
                $(this).show();
            });

            return false;
        }

        $('#MedMenuTable > tbody > tr').each(function () {

            inKey = inKey.toUpperCase().trim();
            let inKeyArr = inKey.split(' ');

            let SearchArr = [];
            SearchArr.push($(this).find('.MedId').text().toUpperCase());
            SearchArr.push($(this).find('.GenericName').text().toUpperCase());
            SearchArr.push($(this).find('.BrandName').text().toUpperCase());

            $(this).hide();
            for (let i = 0; i < SearchArr.length; i++) {
                let isMatch = false;
                for (let j = 0; j < inKeyArr.length; j++) {
                    if (SearchArr[i].includes(inKeyArr[j])) {
                        isMatch = true;
                    } else {
                        isMatch = false;
                        break;
                    };
                }
                if (isMatch == true) {
                    $(this).show();
                }
            };
        });
    });

    $("#NonMedMenuSearch").autocomplete({
        source: function (request, response) {

            let inKey = $('#NonMedMenuSearch').val();
            let inType = $(".MenuTabs").find('.nav-item .nav-link.active').text();
            // Convert String Laboratory -> Lab 、 Radiology -> Exam 、 Pathology -> Path
            inType = inType == 'Laboratory' ? 'Lab' : inType == 'Radiology' ? "Exam" : inType == 'Pathology' ? 'Path' : inType == 'Material' ? "Supply" : inType;

            // if (inKey.length < 3) { return false; }; 

            $.ajax({
                type: 'POST',
                url: Root + "InputHelper/SearchNonMed",
                data: {
                    inKey: inKey,
                    inType: inType,
                },
                async: false,
                dataType: 'json',
                success: function (result) {
                    if (result != null) {
                        response(result);
                    }
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    console.log(thrownError);
                },
                complete: function (XMLHttpRequest, textStatus) { }
            });
        }
    })

    /* key enter to add item*/
    $("#NonMedMenuSearch").keypress(function (e) {
        var key = window.event ? e.keyCode : e.which;
        if (key == 13) {
            let input = $('#NonMedMenuSearch').val();
            let KeyArr = input.split(' ');
            // let PlanCode = KeyArr[KeyArr.length - 1];
            let PlanCode = KeyArr[0];

            // Search planCode to add Item.
            let inType = $(".MenuTabs").find('.nav-item .nav-link.active').text();
            if (inType != 'Material') {
                $('.NonMedMenu .NonMedNodes > .ItemId').each(function () {
                    if ($(this).text() == PlanCode) {
                        $(this).parent().dblclick();
                    }
                })
            } else {
                $('#SupplyMenu .SupplyMenutr > .ItemId').each(function () {
                    if ($(this).text().trim() == PlanCode) {
                        $(this).parent().dblclick();
                    }
                })
            }
        }
    });

    // Switch Search Input by Tabs
    $(".MenuTabs").find('.nav-item .nav-link').click(function () {
        let MenuSearch = $(this).data('search');
        $('#SearchGroups .top_search').hide();
        $('#' + MenuSearch).show();
    });
});
